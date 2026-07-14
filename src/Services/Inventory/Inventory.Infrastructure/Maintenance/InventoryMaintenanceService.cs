using BuildingBlocks.Application;
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
    /// Deletes processed Inbox rows and processed Outbox rows older than the retention window.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var acquired = await db.Database
            .SqlQueryRaw<bool>($"select pg_try_advisory_xact_lock({CleanupLockKey}) as \"Value\"")
            .SingleAsync(cancellationToken);

        if (!acquired)
        {
            logger.LogDebug("Inventory cleanup skipped because another instance holds the lock");
            return;
        }

        var cutoff = clock.UtcNow.Subtract(Retention);
        var inboxDeleted = await db.Inbox
            .Where(x => x.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
        var outboxDeleted = await db.Outbox
            .Where(x => x.ProcessedAt != null && x.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation(
            "Inventory cleanup deleted {InboxDeleted} inbox rows and {OutboxDeleted} outbox rows older than {Cutoff}",
            inboxDeleted,
            outboxDeleted,
            cutoff);
    }
}
