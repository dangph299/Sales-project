using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure;

/// <summary>
/// Hangfire adapter that deletes Inventory processed inbox rows past their retention window.
/// </summary>
public sealed class InboxCleanupJob(
    InventoryDbContext db,
    IClock clock,
    IOptions<InventoryRecurringJobsOptions> options,
    ILogger<InboxCleanupJob> logger) : InboxCleanupJobBase<InventoryDbContext>(db, logger)
{
    /// <summary>
    /// Executes one Inventory processed inbox cleanup batch. No distributed lock is used: the
    /// delete is bounded and idempotent, so concurrent or repeated runs cannot corrupt data.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value.InboxCleanup;
        await ExecuteCleanupBatchAsync(
            jobOptions.BatchSize,
            jobOptions.RetentionDays,
            clock.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    protected override DbSet<InboxMessage> Inbox(InventoryDbContext dbContext) => dbContext.Inbox;

    /// <inheritdoc/>
    protected override void RecordDeleted(long count) => InventoryMetrics.InboxCleanupDeleted.Add(count);
}
