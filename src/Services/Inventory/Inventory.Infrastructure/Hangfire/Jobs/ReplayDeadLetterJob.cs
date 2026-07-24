using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Infrastructure.Coordination.Redis;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure;

/// <summary>
/// Hangfire adapter that resets Inventory inbound dead-lettered messages for inbox re-drive.
/// </summary>
public sealed class ReplayDeadLetterJob(
    InventoryDbContext db,
    IClock clock,
    IDistributedLeaseManager distributedLeaseManager,
    IOptions<InventoryRecurringJobsOptions> options,
    ILogger<ReplayDeadLetterJob> logger) : InboxDeadLetterReplayJobBase<InventoryDbContext>(db, logger)
{
    private static readonly DistributedLeaseOptions ReplayDeadLetterLeaseOptions = new()
    {
        LeaseDuration = TimeSpan.FromMinutes(5)
    };

    /// <summary>
    /// Executes one Inventory inbound dead-letter replay batch. Single-instance execution is
    /// coordinated with a Redis distributed lease rather than a PostgreSQL advisory lock, because
    /// replay must not run concurrently on more than one Inventory instance even though the
    /// underlying row reset is itself claimed atomically per row.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await distributedLeaseManager.TryAcquireAsync(
            InventoryMessagingJobResources.ReplayDeadLetter,
            ReplayDeadLetterLeaseOptions,
            cancellationToken);
        if (lease is null)
        {
            logger.LogDebug("Inventory replay dead-letter job skipped because another instance holds the lease");
            return;
        }

        var jobOptions = options.Value.ReplayDeadLetter;
        await ExecuteReplayBatchAsync(
            jobOptions.BatchSize,
            jobOptions.RetryDelaySeconds,
            clock.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override DbSet<InboxMessage> Inbox(InventoryDbContext dbContext) => dbContext.Inbox;

    /// <inheritdoc/>
    protected override void RecordReplayRequested(long count) => InventoryMetrics.InboxDeadLetterReplayRequested.Add(count);
}
