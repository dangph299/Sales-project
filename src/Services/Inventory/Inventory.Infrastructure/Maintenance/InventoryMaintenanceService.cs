using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure;

/// <summary>
/// Maintenance operations for Inventory persistence tables.
/// </summary>
public sealed class InventoryMaintenanceService(
    InventoryDbContext db,
    IClock clock,
    ILogger<InventoryMaintenanceService> logger)
{
    private const long CleanupLockKey = 7_281_001_001;
    private static readonly TimeSpan Retention = TimeSpan.FromDays(14);

    /// <summary>
    /// Deletes processed Outbox rows older than the retention window.
    /// </summary>
    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var acquired = await db.Database
            .SqlQueryRaw<bool>("select pg_try_advisory_xact_lock({0}) as \"Value\"", CleanupLockKey)
            .SingleAsync(cancellationToken);

        if (!acquired)
        {
            logger.LogDebug("Inventory cleanup skipped because another instance holds the lock");
            return;
        }

        var cutoff = clock.UtcNow.Subtract(Retention);
        var outboxDeleted = await db.Outbox
            .Where(x => x.ProcessedAt != null && x.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation(
            "Inventory cleanup deleted {OutboxDeleted} processed outbox rows older than {Cutoff}",
            outboxDeleted,
            cutoff);
    }

    /// <summary>
    /// Resets a single inbound dead-lettered message so a Kafka/DLQ replay can process it again.
    /// </summary>
    /// <param name="eventId">Inbound event id to reset.</param>
    /// <returns><see langword="true"/> if a matching dead-lettered inbox row was reset; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ResetInboxDeadLetterAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var row = await db.Inbox.SingleOrDefaultAsync(x =>
            x.EventId == eventId &&
            x.Status == InboxMessageStatus.DeadLettered,
            cancellationToken);
        if (row is null) return false;

        ResetInboxForReplay(row);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Resets up to <paramref name="take"/> inbound dead-lettered messages so Kafka/DLQ replay can process them again.
    /// </summary>
    /// <param name="take">Maximum number of dead-lettered messages to reset. Clamped between 1 and 100.</param>
    /// <returns>Number of inbox rows that were reset.</returns>
    public async Task<int> ResetInboxDeadLettersAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        var rows = await db.Inbox
            .Where(x => x.Status == InboxMessageStatus.DeadLettered)
            .OrderBy(x => x.DeadLetteredAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        foreach (var row in rows) ResetInboxForReplay(row);
        await db.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    private static void ResetInboxForReplay(InboxMessage row)
    {
        row.Status = InboxMessageStatus.Failed;
        row.Attempts = 0;
        row.LastError = null;
        row.LastExceptionType = null;
        row.LastFailedAt = null;
        row.DeadLetteredAt = null;
    }
}
