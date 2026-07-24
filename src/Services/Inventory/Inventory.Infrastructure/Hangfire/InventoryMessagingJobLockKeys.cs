namespace Inventory.Infrastructure;

/// <summary>
/// Stable PostgreSQL advisory lock keys for Inventory messaging recurring jobs.
/// </summary>
public static class InventoryMessagingJobLockKeys
{
    /// <summary>Lock key for Inventory inbound dead-letter replay.</summary>
    public const long ReplayDeadLetter = 7_281_003_001;

    /// <summary>Lock key for Inventory processed inbox cleanup.</summary>
    public const long InboxCleanup = 7_281_003_002;

    /// <summary>Lock key for Inventory failed outbox retry reset.</summary>
    public const long FailedOutboxRetry = 7_281_003_003;

    /// <summary>Lock key for Inventory outbox pending monitor.</summary>
    public const long OutboxPendingMonitor = 7_281_003_004;

    /// <summary>Lock key for Inventory Kafka lag monitor.</summary>
    public const long KafkaLagMonitor = 7_281_003_005;
}
