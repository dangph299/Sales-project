using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
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
    IOptions<InventoryRecurringJobsOptions> options,
    ILogger<ReplayDeadLetterJob> logger) : InboxDeadLetterReplayJobBase<InventoryDbContext>(db, logger)
{
    /// <summary>
    /// Executes one Inventory inbound dead-letter replay batch.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value.ReplayDeadLetter;
        await ExecuteCoreAsync(
            jobOptions.BatchSize,
            jobOptions.RetryDelaySeconds,
            InventoryMessagingJobLockKeys.ReplayDeadLetter,
            clock.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override DbSet<InboxMessage> Inbox(InventoryDbContext dbContext) => dbContext.Inbox;

    /// <inheritdoc/>
    protected override void RecordReplayRequested(long count) => InventoryMetrics.InboxDeadLetterReplayRequested.Add(count);
}
