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
    /// Executes one Inventory terminal failed outbox retry reset batch. No distributed lock is
    /// used: rows are claimed with an atomic conditional update whose WHERE clause re-validates
    /// eligibility (<c>LockedUntil</c>), so concurrent callers cannot both reset the same row.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value.FailedOutboxRetry;
        await ExecuteRetryBatchAsync(
            jobOptions.BatchSize,
            jobOptions.RetryDelaySeconds,
            clock.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override DbSet<OutboxMessage> Outbox(InventoryDbContext dbContext) => dbContext.Outbox;

    /// <inheritdoc/>
    protected override void RecordRetryRequested(long count) => InventoryMetrics.OutboxRetryRequested.Add(count);
}
