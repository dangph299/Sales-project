using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Shared Hangfire job body for resetting inbound dead-letter rows so each service's inbox
/// re-drive loop owns the actual replay.
/// </summary>
public abstract class InboxDeadLetterReplayJobBase<TDbContext>(
    TDbContext db,
    ILogger logger)
    where TDbContext : DbContext
{
    /// <summary>Returns the service-owned inbox table.</summary>
    protected abstract DbSet<InboxMessage> Inbox(TDbContext dbContext);

    /// <summary>Records how many rows were reset for re-drive.</summary>
    protected abstract void RecordReplayRequested(long count);

    /// <summary>Executes one reset batch.</summary>
    protected async Task ExecuteCoreAsync(
        int batchSize,
        int retryDelaySeconds,
        long lockKey,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await TryAcquireLockAsync(lockKey, cancellationToken))
        {
            logger.LogDebug("{DbContext} replay dead-letter job skipped because another instance holds the lock", typeof(TDbContext).Name);
            return;
        }

        await ExecuteReplayBatchAsync(batchSize, retryDelaySeconds, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Executes one reset batch without opening a transaction or acquiring any lock. Callers that
    /// need single-instance execution must coordinate that themselves (e.g. a distributed lease)
    /// before invoking this method.
    /// </summary>
    protected async Task ExecuteReplayBatchAsync(
        int batchSize,
        int retryDelaySeconds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var nextAttemptAt = now.AddSeconds(retryDelaySeconds);
        var ids = await Inbox(db)
            .Where(row => row.Status == InboxMessageStatus.DeadLettered && row.Payload != null)
            .OrderBy(row => row.DeadLetteredAt)
            .Select(row => row.EventId)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);

        var reset = await Inbox(db)
            .Where(row => ids.Contains(row.EventId) && row.Status == InboxMessageStatus.DeadLettered)
            .ExecuteUpdateAsync(update => update
                .SetProperty(row => row.Status, InboxMessageStatus.Failed)
                .SetProperty(row => row.Attempts, 0)
                .SetProperty(row => row.LastError, (string?)null)
                .SetProperty(row => row.LastExceptionType, (string?)null)
                .SetProperty(row => row.LastFailedAt, (DateTimeOffset?)null)
                .SetProperty(row => row.DeadLetteredAt, (DateTimeOffset?)null)
                .SetProperty(row => row.NextAttemptAt, nextAttemptAt), cancellationToken);

        RecordReplayRequested(reset);
        logger.LogInformation(
            "{DbContext} replay dead-letter job reset {ResetCount} inbox rows at {ReplayRequestedAt} for retry at {NextAttemptAt}",
            typeof(TDbContext).Name,
            reset,
            now,
            nextAttemptAt);
    }

    private async Task<bool> TryAcquireLockAsync(long lockKey, CancellationToken cancellationToken)
    {
        return await db.Database
            .SqlQueryRaw<bool>("select pg_try_advisory_xact_lock({0}) as \"Value\"", lockKey)
            .SingleAsync(cancellationToken);
    }
}
