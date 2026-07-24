using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Shared Hangfire job body for deleting processed inbox rows past a service retention window.
/// </summary>
public abstract class InboxCleanupJobBase<TDbContext>(
    TDbContext db,
    ILogger logger)
    where TDbContext : DbContext
{
    /// <summary>Returns the service-owned inbox table.</summary>
    protected abstract DbSet<InboxMessage> Inbox(TDbContext dbContext);

    /// <summary>Records how many processed rows were deleted.</summary>
    protected abstract void RecordDeleted(long count);

    /// <summary>Executes one cleanup batch.</summary>
    protected async Task ExecuteCoreAsync(
        int batchSize,
        int retentionDays,
        long lockKey,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await TryAcquireLockAsync(lockKey, cancellationToken))
        {
            logger.LogDebug("{DbContext} inbox cleanup skipped because another instance holds the lock", typeof(TDbContext).Name);
            return;
        }

        await ExecuteCleanupBatchAsync(batchSize, retentionDays, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Executes one cleanup batch without opening a transaction or acquiring any lock. Deletes are
    /// bounded and idempotent, so concurrent or repeated invocations are safe without coordination.
    /// </summary>
    protected async Task ExecuteCleanupBatchAsync(
        int batchSize,
        int retentionDays,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var cutoff = now.AddDays(-retentionDays);
        var ids = await Inbox(db)
            .Where(row => row.Status == InboxMessageStatus.Processed && row.ProcessedAt < cutoff)
            .OrderBy(row => row.ProcessedAt)
            .Select(row => row.EventId)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);

        var deleted = await Inbox(db)
            .Where(row => ids.Contains(row.EventId))
            .ExecuteDeleteAsync(cancellationToken);

        RecordDeleted(deleted);
        logger.LogInformation(
            "{DbContext} inbox cleanup deleted {DeletedCount} processed rows older than {Cutoff}",
            typeof(TDbContext).Name,
            deleted,
            cutoff);
    }

    private async Task<bool> TryAcquireLockAsync(long lockKey, CancellationToken cancellationToken)
    {
        return await db.Database
            .SqlQueryRaw<bool>("select pg_try_advisory_xact_lock({0}) as \"Value\"", lockKey)
            .SingleAsync(cancellationToken);
    }
}
