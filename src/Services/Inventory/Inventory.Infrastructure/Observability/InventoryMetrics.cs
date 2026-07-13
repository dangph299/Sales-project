using System.Diagnostics.Metrics;
using BuildingBlocks.Observability;

namespace Inventory.Infrastructure;

internal static class InventoryMetrics
{
    private static readonly Meter Meter = new("Inventory.Infrastructure");
    private static readonly OutboxMetrics Outbox = new("Inventory.Infrastructure", "inventory");
    private static readonly InboxMetrics Inbox = new("Inventory.Infrastructure", "inventory");

    /// <summary>Counts outbox rows successfully published to Kafka.</summary>
    public static Counter<long> OutboxPublished => Outbox.Published;

    /// <summary>Counts outbox publish attempts that failed.</summary>
    public static Counter<long> OutboxFailed => Outbox.Failed;

    /// <summary>Counts outbox rows that exceeded their maximum publish attempts and were dead-lettered.</summary>
    public static Counter<long> OutboxDeadLettered => Outbox.DeadLettered;

    /// <summary>Counts inbound Kafka messages skipped because they were already recorded in the Inbox.</summary>
    public static Counter<long> InboxDuplicate => Inbox.Duplicate;

    /// <summary>Counts inbound Kafka messages processed successfully for the first time.</summary>
    public static Counter<long> InboxProcessed => Inbox.Processed;

    /// <summary>Counts reservation requests rejected due to insufficient stock.</summary>
    public static readonly Counter<long> ReservationRejected = Meter.CreateCounter<long>("inventory.reservation.rejected");

    /// <summary>Counts reservation requests fulfilled successfully.</summary>
    public static readonly Counter<long> ReservationReserved = Meter.CreateCounter<long>("inventory.reservation.reserved");

    /// <summary>
    /// Updates the observable gauges reporting the current outbox backlog and dead-letter counts.
    /// </summary>
    /// <param name="backlog">Number of outbox rows not yet successfully published or dead-lettered.</param>
    /// <param name="deadLetters">Number of outbox rows currently dead-lettered.</param>
    public static void SetOutboxSnapshot(long backlog, long deadLetters) => Outbox.SetSnapshot(backlog, deadLetters);
}
