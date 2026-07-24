using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure;

/// <summary>
/// Hangfire adapter that reports Inventory outbox backlog health without mutating rows.
/// </summary>
public sealed class OutboxPendingMonitorJob(
    InventoryDbContext db,
    IClock clock,
    IOptions<InventoryRecurringJobsOptions> options,
    ILogger<OutboxPendingMonitorJob> logger) : OutboxPendingMonitorJobBase<InventoryDbContext>(db, logger)
{
    /// <summary>
    /// Executes one Inventory outbox pending health snapshot.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value.OutboxPendingMonitor;
        await ExecuteCoreAsync(
            jobOptions.BacklogWarningThreshold,
            jobOptions.OldestPendingWarningSeconds,
            InventoryMessagingJobLockKeys.OutboxPendingMonitor,
            clock.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override DbSet<OutboxMessage> Outbox(InventoryDbContext dbContext) => dbContext.Outbox;

    /// <inheritdoc/>
    protected override void SetSnapshot(long backlog, long oldestPendingAgeSeconds, long failedTerminal)
        => InventoryMetrics.SetOutboxPendingSnapshot(backlog, oldestPendingAgeSeconds, failedTerminal);
}
