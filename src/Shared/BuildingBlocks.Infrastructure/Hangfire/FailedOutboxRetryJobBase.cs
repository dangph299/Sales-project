using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Shared Hangfire job body for resetting terminal failed outbox rows so the existing publisher
/// owns the actual retry.
/// </summary>
public abstract class FailedOutboxRetryJobBase<TDbContext>(
    TDbContext db,
    IOutboxSignal outboxSignal,
    ILogger logger)
    where TDbContext : DbContext
{
    private static readonly TimeSpan ClaimDuration = TimeSpan.FromMinutes(5);

    /// <summary>Returns the service-owned outbox table.</summary>
    protected abstract DbSet<OutboxMessage> Outbox(TDbContext dbContext);

    /// <summary>Records how many rows were reset for publisher retry.</summary>
    protected abstract void RecordRetryRequested(long count);

    /// <summary>Executes one terminal failed outbox reset batch.</summary>
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
            logger.LogDebug("{DbContext} failed outbox retry skipped because another instance holds the lock", typeof(TDbContext).Name);
            return;
        }

        var claimId = Guid.NewGuid();
        var ids = await Outbox(db)
            .Where(row => row.ProcessedAt == null &&
                row.DeadLetteredAt != null &&
                (row.LockedUntil == null || row.LockedUntil < now))
            .OrderBy(row => row.DeadLetteredAt)
            .Select(row => row.Id)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);

        await Outbox(db)
            .Where(row => ids.Contains(row.Id) &&
                row.ProcessedAt == null &&
                row.DeadLetteredAt != null &&
                (row.LockedUntil == null || row.LockedUntil < now))
            .ExecuteUpdateAsync(update => update
                .SetProperty(row => row.LockId, claimId)
                .SetProperty(row => row.LockedUntil, now.Add(ClaimDuration)), cancellationToken);

        var nextAttemptAt = now.AddSeconds(retryDelaySeconds);
        var reset = await Outbox(db)
            .Where(row => row.LockId == claimId && row.ProcessedAt == null && row.DeadLetteredAt != null)
            .ExecuteUpdateAsync(update => update
                .SetProperty(row => row.Attempts, 0)
                .SetProperty(row => row.LastError, (string?)null)
                .SetProperty(row => row.NextAttemptAt, nextAttemptAt)
                .SetProperty(row => row.DeadLetteredAt, (DateTimeOffset?)null)
                .SetProperty(row => row.LockId, (Guid?)null)
                .SetProperty(row => row.LockedUntil, (DateTimeOffset?)null), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        if (reset > 0)
        {
            outboxSignal.Notify();
        }

        RecordRetryRequested(reset);
        logger.LogInformation(
            "{DbContext} failed outbox retry reset {ResetCount} outbox rows at {RetryRequestedAt} for retry at {NextAttemptAt}",
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
