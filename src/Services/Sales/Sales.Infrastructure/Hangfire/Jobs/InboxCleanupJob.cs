using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

/// <summary>
/// Hangfire adapter that deletes processed inbox rows past their retention window.
/// </summary>
public sealed class InboxCleanupJob(
    SalesDbContext db,
    IClock clock,
    IOptions<SalesRecurringJobsOptions> options,
    ILogger<InboxCleanupJob> logger) : InboxCleanupJobBase<SalesDbContext>(db, logger)
{
    /// <summary>
    /// Executes one processed inbox cleanup batch.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value.InboxCleanup;
        await ExecuteCoreAsync(
            jobOptions.BatchSize,
            jobOptions.RetentionDays,
            SalesMessagingJobLockKeys.InboxCleanup,
            clock.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override DbSet<InboxMessage> Inbox(SalesDbContext dbContext) => dbContext.InboxMessages;

    /// <inheritdoc/>
    protected override void RecordDeleted(long count) => SalesMetrics.InboxCleanupDeleted.Add(count);
}
