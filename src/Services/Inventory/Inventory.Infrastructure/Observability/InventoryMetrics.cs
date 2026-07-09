using System.Diagnostics.Metrics;

namespace Inventory.Infrastructure;

/// <summary>
/// Custom OpenTelemetry metrics for Inventory's outbox/inbox pipeline and reservation outcomes.
/// Registered into the metrics provider via <c>AddMeter("Inventory.Infrastructure")</c>.
/// </summary>
internal static class InventoryMetrics
{
    private static readonly Meter Meter = new("Inventory.Infrastructure");

    /// <summary>Counts outbox rows successfully published to Kafka.</summary>
    public static readonly Counter<long> OutboxPublished = Meter.CreateCounter<long>("inventory.outbox.published");

    /// <summary>Counts outbox publish attempts that failed.</summary>
    public static readonly Counter<long> OutboxFailed = Meter.CreateCounter<long>("inventory.outbox.failed");

    /// <summary>Counts outbox rows that exceeded their maximum publish attempts and were dead-lettered.</summary>
    public static readonly Counter<long> OutboxDeadLettered = Meter.CreateCounter<long>("inventory.outbox.deadlettered");

    /// <summary>Counts inbound Kafka messages skipped because they were already recorded in the Inbox.</summary>
    public static readonly Counter<long> InboxDuplicate = Meter.CreateCounter<long>("inventory.inbox.duplicate");

    /// <summary>Counts inbound Kafka messages processed successfully for the first time.</summary>
    public static readonly Counter<long> InboxProcessed = Meter.CreateCounter<long>("inventory.inbox.processed");

    /// <summary>Counts reservation requests rejected due to insufficient stock.</summary>
    public static readonly Counter<long> ReservationRejected = Meter.CreateCounter<long>("inventory.reservation.rejected");

    /// <summary>Counts reservation requests fulfilled successfully.</summary>
    public static readonly Counter<long> ReservationReserved = Meter.CreateCounter<long>("inventory.reservation.reserved");

    private static long _outboxBacklog;
    private static long _outboxDeadLetters;

    static InventoryMetrics()
    {
        Meter.CreateObservableGauge("inventory.outbox.backlog", () => Interlocked.Read(ref _outboxBacklog));
        Meter.CreateObservableGauge("inventory.outbox.deadletters", () => Interlocked.Read(ref _outboxDeadLetters));
    }

    /// <summary>
    /// Updates the observable gauges reporting the current outbox backlog and dead-letter counts.
    /// </summary>
    /// <param name="backlog">
    /// The number of outbox rows not yet successfully published or dead-lettered.
    /// </param>
    /// <param name="deadLetters">
    /// The number of outbox rows currently dead-lettered.
    /// </param>
    public static void SetOutboxSnapshot(long backlog, long deadLetters)
    {
        Interlocked.Exchange(ref _outboxBacklog, backlog);
        Interlocked.Exchange(ref _outboxDeadLetters, deadLetters);
    }
}
