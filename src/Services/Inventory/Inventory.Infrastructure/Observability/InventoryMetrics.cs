using System.Diagnostics.Metrics;
using BuildingBlocks.Infrastructure.Observability.Metrics;

namespace Inventory.Infrastructure;

internal static class InventoryMetrics
{
    private static readonly Meter Meter = new("Inventory.Infrastructure");
    private static readonly OutboxMetrics Outbox = new("Inventory.Infrastructure", "inventory");
    private static readonly InboxMetrics Inbox = new("Inventory.Infrastructure", "inventory");
    private static long _kafkaConsumerLag;
    private static long _kafkaConsumerLagPartitions;

    static InventoryMetrics()
    {
        Meter.CreateObservableGauge("inventory.kafka.consumer_lag", () => Interlocked.Read(ref _kafkaConsumerLag));
        Meter.CreateObservableGauge("inventory.kafka.consumer_lag_partitions", () => Interlocked.Read(ref _kafkaConsumerLagPartitions));
    }

    /// <summary>Counts outbox rows successfully published to Kafka.</summary>
    public static Counter<long> OutboxPublished => Outbox.Published;

    /// <summary>Counts outbox publish attempts that failed.</summary>
    public static Counter<long> OutboxFailed => Outbox.Failed;

    /// <summary>Counts outbox rows that exceeded their maximum publish attempts and were dead-lettered.</summary>
    public static Counter<long> OutboxDeadLettered => Outbox.DeadLettered;

    /// <summary>Counts terminal failed outbox rows reset so the publisher can retry them.</summary>
    public static Counter<long> OutboxRetryRequested => Outbox.RetryRequested;

    /// <summary>Counts inbound Kafka messages skipped because they were already recorded in the Inbox.</summary>
    public static Counter<long> InboxDuplicate => Inbox.Duplicate;

    /// <summary>Counts inbound Kafka messages processed successfully for the first time.</summary>
    public static Counter<long> InboxProcessed => Inbox.Processed;

    /// <summary>Counts previously failed inbound messages re-driven to success.</summary>
    public static Counter<long> InboxRetried => Inbox.Retried;

    /// <summary>Counts inbound messages dead-lettered after exhausting re-drive attempts.</summary>
    public static Counter<long> InboxDeadLettered => Inbox.DeadLettered;

    /// <summary>Counts processed inbox rows deleted by retention cleanup.</summary>
    public static Counter<long> InboxCleanupDeleted => Inbox.CleanupDeleted;

    /// <summary>Counts inbox dead-letter rows reset so the re-drive service can replay them.</summary>
    public static Counter<long> InboxDeadLetterReplayRequested => Inbox.DeadLetterReplayRequested;

    /// <summary>Counts reservation requests rejected due to insufficient stock.</summary>
    public static readonly Counter<long> ReservationRejected = Meter.CreateCounter<long>("inventory.reservation.rejected");

    /// <summary>Counts reservation requests fulfilled successfully.</summary>
    public static readonly Counter<long> ReservationReserved = Meter.CreateCounter<long>("inventory.reservation.reserved");

    /// <summary>
    /// Updates the observable gauges reporting the current outbox backlog and dead-letter counts.
    /// </summary>
    /// <param name="backlog">Number of outbox rows not yet successfully published or dead-lettered.</param>
    /// <param name="deadLetters">Number of outbox rows currently dead-lettered.</param>
    public static void SetOutboxSnapshot(long backlog, long deadLetters)
    {
        Outbox.SetSnapshot(backlog, deadLetters);
    }

    /// <summary>Updates outbox monitor gauges.</summary>
    public static void SetOutboxPendingSnapshot(long backlog, long oldestPendingAgeSeconds, long failedTerminal)
    {
        Outbox.SetPendingSnapshot(backlog, oldestPendingAgeSeconds, failedTerminal);
    }

    /// <summary>Updates Kafka consumer lag gauges.</summary>
    public static void SetKafkaConsumerLag(long lag, long partitions)
    {
        Interlocked.Exchange(ref _kafkaConsumerLag, lag);
        Interlocked.Exchange(ref _kafkaConsumerLagPartitions, partitions);
    }
}
