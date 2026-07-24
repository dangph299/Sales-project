namespace Inventory.Infrastructure;

/// <summary>
/// Stable recurring job identifiers owned by the Inventory service.
/// </summary>
public static class InventoryRecurringJobIds
{
    /// <summary>Recurring job identifier for Inventory inbound dead-letter replay.</summary>
    public const string ReplayDeadLetter = "inventory-dead-letter-replay";

    /// <summary>Recurring job identifier for Inventory Kafka lag monitoring.</summary>
    public const string KafkaLagMonitor = "inventory-kafka-lag-monitor";

    /// <summary>Recurring job identifier for Inventory processed inbox cleanup.</summary>
    public const string InboxCleanup = "inventory-inbox-cleanup";

    /// <summary>Recurring job identifier for Inventory failed outbox retry reset.</summary>
    public const string FailedOutboxRetry = "inventory-failed-outbox-retry";

    /// <summary>Recurring job identifier for Inventory outbox pending health snapshots.</summary>
    public const string OutboxPendingMonitor = "inventory-outbox-pending-monitor";
}
