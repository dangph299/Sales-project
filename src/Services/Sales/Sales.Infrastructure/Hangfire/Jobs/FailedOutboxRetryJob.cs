using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

/// <summary>
/// Hangfire adapter that resets terminal failed outbox rows so the outbox publisher can retry them.
/// </summary>
public sealed class FailedOutboxRetryJob(
    SalesDbContext db,
    IClock clock,
    IOutboxSignal outboxSignal,
    IOptions<SalesRecurringJobsOptions> options,
    ILogger<FailedOutboxRetryJob> logger) : FailedOutboxRetryJobBase<SalesDbContext>(db, outboxSignal, logger)
{
    /// <summary>
    /// Executes one terminal failed outbox retry reset batch.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value.FailedOutboxRetry;
        await ExecuteCoreAsync(
            jobOptions.BatchSize,
            jobOptions.RetryDelaySeconds,
            SalesMessagingJobLockKeys.FailedOutboxRetry,
            clock.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override DbSet<OutboxMessage> Outbox(SalesDbContext dbContext) => dbContext.OutboxMessages;

    /// <inheritdoc/>
    protected override void RecordRetryRequested(long count) => SalesMetrics.OutboxRetryRequested.Add(count);
}
