using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Shared Hangfire job body for reading outbox pending health without publishing or mutating rows.
/// </summary>
public abstract class OutboxPendingMonitorJobBase<TDbContext>(
    TDbContext db,
    ILogger logger)
    where TDbContext : DbContext
{
    /// <summary>Returns the service-owned outbox table.</summary>
    protected abstract DbSet<OutboxMessage> Outbox(TDbContext dbContext);

    /// <summary>Updates service-specific outbox monitor gauges.</summary>
    protected abstract void SetSnapshot(long backlog, long oldestPendingAgeSeconds, long failedTerminal);

    /// <summary>Executes one outbox pending snapshot.</summary>
    protected async Task ExecuteCoreAsync(
        int backlogWarningThreshold,
        int oldestPendingWarningSeconds,
        long lockKey,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await TryAcquireLockAsync(lockKey, cancellationToken))
        {
            logger.LogDebug("{DbContext} outbox pending monitor skipped because another instance holds the lock", typeof(TDbContext).Name);
            return;
        }

        var pendingQuery = Outbox(db)
            .AsNoTracking()
            .Where(row => row.ProcessedAt == null && row.DeadLetteredAt == null);
        var backlog = await pendingQuery.LongCountAsync(cancellationToken);
        var oldestPendingAt = await pendingQuery.MinAsync(row => (DateTimeOffset?)row.OccurredAt, cancellationToken);
        var failedTerminal = await Outbox(db)
            .AsNoTracking()
            .LongCountAsync(row => row.ProcessedAt == null && row.DeadLetteredAt != null, cancellationToken);

        var oldestAgeSeconds = oldestPendingAt is null
            ? 0
            : Math.Max(0, (long)(now - oldestPendingAt.Value).TotalSeconds);
        SetSnapshot(backlog, oldestAgeSeconds, failedTerminal);

        if (backlog >= backlogWarningThreshold ||
            oldestAgeSeconds >= oldestPendingWarningSeconds)
        {
            logger.LogWarning(
                "{DbContext} outbox pending threshold exceeded {Backlog} {OldestPendingAgeSeconds} {FailedCount}",
                typeof(TDbContext).Name,
                backlog,
                oldestAgeSeconds,
                failedTerminal);
        }
        else
        {
            logger.LogInformation(
                "{DbContext} outbox pending snapshot {Backlog} {OldestPendingAgeSeconds} {FailedCount}",
                typeof(TDbContext).Name,
                backlog,
                oldestAgeSeconds,
                failedTerminal);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<bool> TryAcquireLockAsync(long lockKey, CancellationToken cancellationToken)
    {
        return await db.Database
            .SqlQueryRaw<bool>("select pg_try_advisory_xact_lock({0}) as \"Value\"", lockKey)
            .SingleAsync(cancellationToken);
    }
}
