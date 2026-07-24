using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

/// <summary>
/// Hangfire adapter that resets inbound dead-lettered messages so the inbox re-drive service can replay them.
/// </summary>
public sealed class ReplayDeadLetterJob(
    SalesDbContext db,
    IClock clock,
    IOptions<SalesRecurringJobsOptions> options,
    ILogger<ReplayDeadLetterJob> logger) : InboxDeadLetterReplayJobBase<SalesDbContext>(db, logger)
{
    /// <summary>
    /// Executes one inbound dead-letter replay batch.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value.ReplayDeadLetter;
        await ExecuteCoreAsync(
            jobOptions.BatchSize,
            jobOptions.RetryDelaySeconds,
            SalesMessagingJobLockKeys.ReplayDeadLetter,
            clock.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override DbSet<InboxMessage> Inbox(SalesDbContext dbContext) => dbContext.InboxMessages;

    /// <inheritdoc/>
    protected override void RecordReplayRequested(long count) => SalesMetrics.InboxDeadLetterReplayRequested.Add(count);
}
