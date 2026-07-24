namespace Sales.Infrastructure;

/// <summary>
/// Stable recurring job identifiers owned by the Sales service.
/// </summary>
public static class SalesRecurringJobIds
{
    /// <summary>Recurring job identifier for Sales cleanup.</summary>
    public const string MaintenanceCleanup = "sales-cleanup";

    /// <summary>Recurring job identifier for expired order cancellation.</summary>
    public const string CancelExpiredPendingOrders = "orders:cancel-expired";

    /// <summary>Recurring job identifier for replaying inbound dead-letter messages.</summary>
    public const string ReplayDeadLetter = "messaging:replay-dead-letter";

    /// <summary>Recurring job identifier for monitoring Kafka consumer lag.</summary>
    public const string KafkaLagMonitor = "messaging:kafka-lag-monitor";

    /// <summary>Recurring job identifier for processed inbox cleanup.</summary>
    public const string InboxCleanup = "messaging:inbox-cleanup";

    /// <summary>Recurring job identifier for resetting terminal failed outbox messages.</summary>
    public const string FailedOutboxRetry = "messaging:failed-outbox-retry";

    /// <summary>Recurring job identifier for outbox pending health snapshots.</summary>
    public const string OutboxPendingMonitor = "messaging:outbox-pending-monitor";
}
