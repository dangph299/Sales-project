namespace Inventory.Infrastructure;

/// <summary>
/// Root configuration for every Inventory recurring job.
/// </summary>
public sealed class InventoryRecurringJobsOptions
{
    public const string SectionName = "InventoryRecurringJobs";

    public ReplayDeadLetterJobOptions ReplayDeadLetter { get; init; } = new();

    public KafkaLagMonitorJobOptions KafkaLagMonitor { get; init; } = new();

    public InboxCleanupJobOptions InboxCleanup { get; init; } = new();

    public FailedOutboxRetryJobOptions FailedOutboxRetry { get; init; } = new();

    public OutboxPendingMonitorJobOptions OutboxPendingMonitor { get; init; } = new();
}
