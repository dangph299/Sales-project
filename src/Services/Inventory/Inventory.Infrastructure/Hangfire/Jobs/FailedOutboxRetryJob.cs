using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure;

/// <summary>
/// Hangfire adapter that resets Inventory terminal failed outbox rows for publisher retry.
/// </summary>
public sealed class FailedOutboxRetryJob(
    InventoryDbContext db,
    IClock clock,
    IOutboxSignal outboxSignal,
    IOptions<InventoryRecurringJobsOptions> options,
    ILogger<FailedOutboxRetryJob> logger) : FailedOutboxRetryJobBase<InventoryDbContext>(db, outboxSignal, logger)
{
    /// <summary>
    /// Executes one Inventory terminal failed outbox retry reset batch.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value.FailedOutboxRetry;
        await ExecuteCoreAsync(
            jobOptions.BatchSize,
            jobOptions.RetryDelaySeconds,
            InventoryMessagingJobLockKeys.FailedOutboxRetry,
            clock.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override DbSet<OutboxMessage> Outbox(InventoryDbContext dbContext) => dbContext.Outbox;

    /// <inheritdoc/>
    protected override void RecordRetryRequested(long count) => InventoryMetrics.OutboxRetryRequested.Add(count);
}
