using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Infrastructure.Coordination.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Sales.Infrastructure;

/// <summary>
/// Hangfire adapter that deletes processed Inbox and Outbox rows past their retention window.
/// </summary>
public sealed class MaintenanceCleanupJob(
    SalesDbContext db,
    IDistributedLeaseManager distributedLeaseManager,
    IClock clock,
    ILogger<MaintenanceCleanupJob> logger)
{
    private const string CleanupLockResource = "jobs:sales:maintenance-cleanup";

    private static readonly DistributedLeaseOptions CleanupLeaseOptions = new()
    {
        LeaseDuration = TimeSpan.FromMinutes(5)
    };

    private static readonly TimeSpan Retention = TimeSpan.FromDays(14);

    /// <summary>
    /// Deletes processed Inbox rows and processed Outbox rows older than the retention window.
    /// Coordinated by a Redis distributed lease so only one instance cleans up per scheduled run.
    /// </summary>
    public async Task CleanupAsync()
    {
        await using var leaseManager = await distributedLeaseManager.TryAcquireAsync(CleanupLockResource, CleanupLeaseOptions);
        if (leaseManager is null)
        {
            logger.LogDebug("Sales maintenance cleanup skipped because another instance holds the lease");
            return;
        }

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
}
