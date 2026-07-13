using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Sales.Infrastructure;

/// <summary>
/// Hangfire recurring/on-demand jobs for maintaining Sales data: cleaning up old Inbox/Outbox rows,
/// and replaying outbox messages that failed or were dead-lettered.
/// </summary>
public sealed class MaintenanceJobs(SalesDbContext db, IConnectionMultiplexer redis, IClock clock)
{
    private const int ReplayBatchLimit = 100;

    /// <summary>
    /// Deletes processed Inbox rows and processed Outbox rows older than 14 days. Coordinated by a
    /// Redis distributed lock so only one running instance performs the cleanup per scheduled run.
    /// </summary>
    public async Task CleanupAsync()
    {
        var cache = redis.GetDatabase();
        var token = Guid.NewGuid().ToString("N");
        const string key = "lock:jobs:sales-cleanup";
        if (!await cache.StringSetAsync(key, token, TimeSpan.FromMinutes(5), When.NotExists)) return;
        try
        {
            var cutoff = clock.UtcNow.AddDays(-14);
            await db.InboxMessages.Where(x => x.ProcessedAt < cutoff).ExecuteDeleteAsync();
            await db.OutboxMessages.Where(x => x.ProcessedAt != null && x.ProcessedAt < cutoff).ExecuteDeleteAsync();
        }
        finally
        {
            await cache.ScriptEvaluateAsync("if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end",
                [key], [token]);
        }
    }

    /// <summary>
    /// Resets a single outbox message so the publisher will attempt to publish it again on its next cycle.
    /// </summary>
    /// <param name="eventId">Outbox message to replay.</param>
    /// <returns><see langword="true"/> if a matching outbox row was found and reset; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ReplayOutboxMessageAsync(Guid eventId)
    {
        var row = await db.OutboxMessages.SingleOrDefaultAsync(x => x.Id == eventId);
        if (row is null) return false;

        ResetForReplay(row);
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Resets up to <paramref name="take"/> dead-lettered outbox messages so the publisher will
    /// attempt to publish them again on its next cycle.
    /// </summary>
    /// <param name="take">Maximum number of dead-lettered messages to reset. Clamped between 1 and 100.</param>
    /// <returns>Number of outbox rows that were reset.</returns>
    public async Task<int> ReplayDeadLettersAsync(int take = ReplayBatchLimit)
    {
        take = Math.Clamp(take, 1, ReplayBatchLimit);
        var rows = await db.OutboxMessages
            .Where(x => x.ProcessedAt == null && x.DeadLetteredAt != null)
            .OrderBy(x => x.DeadLetteredAt)
            .Take(take)
            .ToListAsync();

        foreach (var row in rows) ResetForReplay(row);
        await db.SaveChangesAsync();
        return rows.Count;
    }

    private void ResetForReplay(OutboxMessage row)
    {
        row.Attempts = 0;
        row.LastError = null;
        row.NextAttemptAt = clock.UtcNow;
        row.DeadLetteredAt = null;
        row.LockId = null;
        row.LockedUntil = null;
    }
}
