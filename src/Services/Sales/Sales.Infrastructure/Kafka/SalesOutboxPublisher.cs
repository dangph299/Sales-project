using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sales.Infrastructure;

/// <summary>
/// Background service that polls the Sales outbox every 2 seconds, claims ready rows via a
/// lock/lease, publishes them to Kafka, and tracks retry/backoff/dead-lettering and outbox metrics.
/// </summary>
public sealed class SalesOutboxPublisher(IServiceScopeFactory scopes, IOutboxPublisher publisher, ILogger<SalesOutboxPublisher> logger) : BackgroundService
{
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Runs the outbox publish loop until the host requests a stop.
    /// </summary>
    /// <param name="stoppingToken">
    /// A token that signals when the host is shutting down.
    /// </param>
    /// <returns>
    /// A task representing the long-running background operation.
    /// </returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
                await PublishReadyMessages(db, stoppingToken);
                await UpdateMetrics(db, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Sales outbox cycle failed");
            }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task PublishReadyMessages(SalesDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var lockId = Guid.NewGuid();
        var ids = await db.OutboxMessages
            .Where(x => x.ProcessedAt == null &&
                x.DeadLetteredAt == null &&
                (x.NextAttemptAt == null || x.NextAttemptAt <= now) &&
                (x.LockedUntil == null || x.LockedUntil < now))
            .OrderBy(x => x.OccurredAt)
            .Select(x => x.Id)
            .Take(100)
            .ToArrayAsync(ct);

        if (ids.Length == 0) return;

        await db.OutboxMessages
            .Where(x => ids.Contains(x.Id) && (x.LockedUntil == null || x.LockedUntil < now))
            .ExecuteUpdateAsync(x => x
                .SetProperty(row => row.LockId, lockId)
                .SetProperty(row => row.LockedUntil, now.Add(LockDuration)), ct);

        var messages = await db.OutboxMessages.Where(x => x.LockId == lockId).OrderBy(x => x.OccurredAt).ToListAsync(ct);
        foreach (var row in messages)
        {
            try
            {
                await publisher.PublishAsync(row, ct);
                row.ProcessedAt = DateTimeOffset.UtcNow;
                row.Attempts++;
                row.LastError = null;
                row.LockId = null;
                row.LockedUntil = null;
                SalesMetrics.OutboxPublished.Add(1);
            }
            catch (Exception ex)
            {
                MarkFailed(row, ex);
                SalesMetrics.OutboxFailed.Add(1);
                logger.LogError(ex, "Publish failed {EventId} {RetryCount}", row.Id, row.Attempts);
            }

            await db.SaveChangesAsync(ct);
        }
    }

    private static void MarkFailed(OutboxMessage row, Exception ex)
    {
        row.Attempts++;
        row.LastError = ex.Message[..Math.Min(2000, ex.Message.Length)];
        row.LockId = null;
        row.LockedUntil = null;

        if (row.Attempts >= OutboxMessage.MaxAttempts)
        {
            row.DeadLetteredAt = DateTimeOffset.UtcNow;
            row.NextAttemptAt = null;
            SalesMetrics.OutboxDeadLettered.Add(1);
            return;
        }

        row.NextAttemptAt = DateTimeOffset.UtcNow.Add(Backoff(row.Attempts));
    }

    private static TimeSpan Backoff(int attempts)
    {
        var seconds = Math.Min(300, Math.Pow(2, Math.Min(attempts, 8)));
        return TimeSpan.FromSeconds(seconds);
    }

    private static async Task UpdateMetrics(SalesDbContext db, CancellationToken ct)
    {
        var backlog = await db.OutboxMessages.LongCountAsync(x => x.ProcessedAt == null && x.DeadLetteredAt == null, ct);
        var deadLetters = await db.OutboxMessages.LongCountAsync(x => x.DeadLetteredAt != null, ct);
        SalesMetrics.SetOutboxSnapshot(backlog, deadLetters);
    }
}
