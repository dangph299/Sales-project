using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Base background service for publishing claimed outbox rows to the configured transport.
/// </summary>
public abstract class OutboxPublisherService<TDbContext>(
    IServiceScopeFactory scopes,
    IOutboxPublisher publisher,
    ILogger logger,
    IOutboxSignal signal,
    TimeSpan pollInterval,
    Func<DateTimeOffset> utcNow) : BackgroundService
    where TDbContext : DbContext
{
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);

    /// <summary>Returns the service-owned outbox table.</summary>
    protected abstract DbSet<OutboxMessage> Outbox(TDbContext db);

    /// <summary>Records a successful publish in service metrics.</summary>
    protected abstract void RecordPublished();

    /// <summary>Records a failed publish attempt in service metrics.</summary>
    protected abstract void RecordFailed();

    /// <summary>Records a message moved to dead-letter state in service metrics.</summary>
    protected abstract void RecordDeadLettered();

    /// <summary>Updates service outbox backlog gauges.</summary>
    protected abstract void SetSnapshot(long backlog, long deadLetters);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Each cycle uses a fresh scope because DbContext is scoped and not thread-safe.
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
                await PublishReadyMessages(db, stoppingToken);
                await UpdateMetrics(db, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "{DbContext} outbox cycle failed", typeof(TDbContext).Name);
            }

            // Prefer immediate wake-up from OutboxSignal; fall back to polling for recovery.
            await signal.WaitAsync(pollInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Runs exactly one claim-and-publish cycle against the supplied context. Exposed to reliability
    /// tests so the retry and dead-letter state machine can be exercised deterministically against a
    /// real database without starting the background loop.
    /// </summary>
    internal Task RunPublishCycleAsync(TDbContext db, CancellationToken cancellationToken = default)
        => PublishReadyMessages(db, cancellationToken);

    private async Task PublishReadyMessages(TDbContext db, CancellationToken ct)
    {
        var now = utcNow();
        var lockId = Guid.NewGuid();
        var outbox = Outbox(db);

        // Select ready rows first, then claim them with a lease so multiple app instances do not
        // publish the same message concurrently.
        var ids = await outbox
            .Where(x => x.ProcessedAt == null &&
                x.DeadLetteredAt == null &&
                (x.NextAttemptAt == null || x.NextAttemptAt <= now) &&
                (x.LockedUntil == null || x.LockedUntil < now))
            .OrderBy(x => x.OccurredAt)
            .Select(x => x.Id)
            .Take(100)
            .ToArrayAsync(ct);

        if (ids.Length == 0) return;

        // ExecuteUpdate avoids loading rows just to claim them.
        await outbox
            .Where(x => ids.Contains(x.Id) && (x.LockedUntil == null || x.LockedUntil < now))
            .ExecuteUpdateAsync(x => x
                .SetProperty(row => row.LockId, lockId)
                .SetProperty(row => row.LockedUntil, now.Add(LockDuration)), ct);

        // Load only rows claimed by this cycle's lock id.
        var messages = await outbox.Where(x => x.LockId == lockId).OrderBy(x => x.OccurredAt).ToListAsync(ct);
        foreach (var row in messages)
        {
            await PublishOne(db, row, ct);
        }
    }

    private async Task PublishOne(TDbContext db, OutboxMessage row, CancellationToken ct)
    {
        try
        {
            // Publish first; mark processed only after Kafka acknowledges the message.
            await publisher.PublishAsync(row, ct);
            MarkPublished(row);
            RecordPublished();
        }
        catch (Exception ex)
        {
            MarkFailed(row, ex, utcNow());
            RecordFailed();
            logger.LogError(ex, "Publish failed {EventId} {RetryCount}", row.Id, row.Attempts);
        }

        await db.SaveChangesAsync(ct);
    }

    private void MarkFailed(OutboxMessage row, Exception ex, DateTimeOffset now)
    {
        // Release the lease so a later cycle can retry this message.
        row.Attempts++;
        row.LastError = ex.Message[..Math.Min(2000, ex.Message.Length)];
        row.LockId = null;
        row.LockedUntil = null;

        if (row.Attempts >= OutboxMessage.MaxAttempts)
        {
            // Stop automatic retries after repeated failures; maintenance tooling can replay later.
            row.DeadLetteredAt = now;
            row.NextAttemptAt = null;
            RecordDeadLettered();
            return;
        }

        row.NextAttemptAt = now.Add(Backoff(row.Attempts));
    }

    private void MarkPublished(OutboxMessage row)
    {
        row.ProcessedAt = utcNow();
        row.Attempts++;
        row.LastError = null;
        row.LockId = null;
        row.LockedUntil = null;
    }

    private async Task UpdateMetrics(TDbContext db, CancellationToken ct)
    {
        var outbox = Outbox(db);
        var backlog = await outbox.LongCountAsync(x => x.ProcessedAt == null && x.DeadLetteredAt == null, ct);
        var deadLetters = await outbox.LongCountAsync(x => x.DeadLetteredAt != null, ct);
        SetSnapshot(backlog, deadLetters);
    }

    private static TimeSpan Backoff(int attempts)
    {
        var seconds = Math.Min(300, Math.Pow(2, Math.Min(attempts, 8)));
        return TimeSpan.FromSeconds(seconds);
    }
}
