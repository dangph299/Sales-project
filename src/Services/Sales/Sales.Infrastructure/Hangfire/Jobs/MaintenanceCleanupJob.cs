using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Sales.Infrastructure;

/// <summary>
/// Hangfire adapter that deletes processed Inbox and Outbox rows past their retention window.
/// </summary>
public sealed class MaintenanceCleanupJob(SalesDbContext db, IConnectionMultiplexer redis, IClock clock)
{
    private const string CleanupLockKey = "lock:jobs:sales-cleanup";

    private const string ReleaseLockScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    private static readonly TimeSpan CleanupLockDuration = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan Retention = TimeSpan.FromDays(14);

    /// <summary>
    /// Deletes processed Inbox rows and processed Outbox rows older than the retention window.
    /// Coordinated by a Redis distributed lock so only one instance cleans up per scheduled run.
    /// </summary>
    public async Task CleanupAsync()
    {
        var cache = redis.GetDatabase();
        var lockToken = Guid.NewGuid().ToString("N");

        var lockAcquired = await cache.StringSetAsync(
            CleanupLockKey,
            lockToken,
            CleanupLockDuration,
            When.NotExists);

        if (!lockAcquired)
        {
            return;
        }

        try
        {
            var cutoff = clock.UtcNow.Subtract(Retention);

            await db.InboxMessages
                .Where(inboxMessage => inboxMessage.Status == InboxMessageStatus.Processed
                    && inboxMessage.ProcessedAt < cutoff)
                .ExecuteDeleteAsync();

            await db.OutboxMessages
                .Where(outboxMessage => outboxMessage.ProcessedAt != null
                    && outboxMessage.ProcessedAt < cutoff)
                .ExecuteDeleteAsync();
        }
        finally
        {
            await cache.ScriptEvaluateAsync(ReleaseLockScript, [CleanupLockKey], [lockToken]);
        }
    }
}
