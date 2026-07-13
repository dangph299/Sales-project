using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

/// <summary>
/// Background service that polls the Inventory outbox every 2 seconds, claims ready rows via a
/// lock/lease, publishes them to Kafka, and tracks retry/backoff/dead-lettering and outbox metrics.
/// </summary>
public sealed class InventoryOutboxPublisher(
    IServiceScopeFactory scopes,
    IOutboxPublisher publisher,
    ILogger<InventoryOutboxPublisher> logger,
    IClock clock,
    IOutboxSignal signal,
    IConfiguration configuration) : BackgroundService
{
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);
    private readonly TimeSpan pollInterval = ReadPollInterval(configuration);

    /// <summary>
    /// Runs the outbox publish loop until the host requests a stop.
    /// </summary>
    /// <param name="ct">Host shutdown token.</param>
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                await PublishReadyMessages(db, ct);
                await UpdateMetrics(db, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested) { logger.LogError(ex, "Inventory outbox cycle failed"); }
            await signal.WaitAsync(pollInterval, ct);
        }
    }

    private async Task PublishReadyMessages(InventoryDbContext db, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var lockId = Guid.NewGuid();
        var ids = await db.Outbox
            .Where(x => x.ProcessedAt == null &&
                x.DeadLetteredAt == null &&
                (x.NextAttemptAt == null || x.NextAttemptAt <= now) &&
                (x.LockedUntil == null || x.LockedUntil < now))
            .OrderBy(x => x.OccurredAt)
            .Select(x => x.Id)
            .Take(100)
            .ToArrayAsync(ct);

        if (ids.Length == 0) return;

        await db.Outbox
            .Where(x => ids.Contains(x.Id) && (x.LockedUntil == null || x.LockedUntil < now))
            .ExecuteUpdateAsync(x => x
                .SetProperty(row => row.LockId, lockId)
                .SetProperty(row => row.LockedUntil, now.Add(LockDuration)), ct);

        var rows = await db.Outbox.Where(x => x.LockId == lockId).OrderBy(x => x.OccurredAt).ToListAsync(ct);
        foreach (var row in rows)
        {
            try
            {
                await publisher.PublishAsync(row, ct);
                row.ProcessedAt = clock.UtcNow;
                row.Attempts++;
                row.LastError = null;
                row.LockId = null;
                row.LockedUntil = null;
                InventoryMetrics.OutboxPublished.Add(1);
            }
            catch (Exception ex)
            {
                MarkFailed(row, ex, clock.UtcNow);
                InventoryMetrics.OutboxFailed.Add(1);
                logger.LogError(ex, "Publish failed {EventId} {RetryCount}", row.Id, row.Attempts);
            }
            await db.SaveChangesAsync(ct);
        }
    }

    private static void MarkFailed(OutboxMessage row, Exception ex, DateTimeOffset now)
    {
        row.Attempts++;
        row.LastError = ex.Message[..Math.Min(2000, ex.Message.Length)];
        row.LockId = null;
        row.LockedUntil = null;

        if (row.Attempts >= OutboxMessage.MaxAttempts)
        {
            row.DeadLetteredAt = now;
            row.NextAttemptAt = null;
            InventoryMetrics.OutboxDeadLettered.Add(1);
            return;
        }

        row.NextAttemptAt = now.Add(Backoff(row.Attempts));
    }

    private static TimeSpan Backoff(int attempts)
    {
        var seconds = Math.Min(300, Math.Pow(2, Math.Min(attempts, 8)));
        return TimeSpan.FromSeconds(seconds);
    }

    private static async Task UpdateMetrics(InventoryDbContext db, CancellationToken ct)
    {
        var backlog = await db.Outbox.LongCountAsync(x => x.ProcessedAt == null && x.DeadLetteredAt == null, ct);
        var deadLetters = await db.Outbox.LongCountAsync(x => x.DeadLetteredAt != null, ct);
        InventoryMetrics.SetOutboxSnapshot(backlog, deadLetters);
    }

    private static TimeSpan ReadPollInterval(IConfiguration configuration)
    {
        var milliseconds = configuration.GetValue("Outbox:PollIntervalMilliseconds", 2_000);
        return TimeSpan.FromMilliseconds(Math.Clamp(milliseconds, 100, 60_000));
    }
}
