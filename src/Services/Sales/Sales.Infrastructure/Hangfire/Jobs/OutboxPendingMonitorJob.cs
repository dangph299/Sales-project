using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

/// <summary>
/// Hangfire adapter that reports outbox backlog health without publishing or mutating outbox rows.
/// </summary>
public sealed class OutboxPendingMonitorJob(
    SalesDbContext db,
    IClock clock,
    IOptions<SalesRecurringJobsOptions> options,
    ILogger<OutboxPendingMonitorJob> logger) : OutboxPendingMonitorJobBase<SalesDbContext>(db, logger)
{
    /// <summary>
    /// Executes one outbox pending health snapshot.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value.OutboxPendingMonitor;
        await ExecuteCoreAsync(
            jobOptions.BacklogWarningThreshold,
            jobOptions.OldestPendingWarningSeconds,
            SalesMessagingJobLockKeys.OutboxPendingMonitor,
            clock.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override DbSet<OutboxMessage> Outbox(SalesDbContext dbContext) => dbContext.OutboxMessages;

    /// <inheritdoc/>
    protected override void SetSnapshot(long backlog, long oldestPendingAgeSeconds, long failedTerminal)
        => SalesMetrics.SetOutboxPendingSnapshot(backlog, oldestPendingAgeSeconds, failedTerminal);
}
