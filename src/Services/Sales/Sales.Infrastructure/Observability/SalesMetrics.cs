using System.Diagnostics.Metrics;
using BuildingBlocks.Infrastructure.Observability.Metrics;

namespace Sales.Infrastructure;

internal static class SalesMetrics
{
    private const string MeterName = "Sales.Infrastructure";

    private static readonly OutboxMetrics Outbox = new(MeterName, "sales");
    private static readonly InboxMetrics Inbox = new(MeterName, "sales");

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> ExpiredOrdersScanned =
        Meter.CreateCounter<long>("sales.orders.expiration.scanned");
    private static readonly Counter<long> ExpiredOrdersCancelled =
        Meter.CreateCounter<long>("sales.orders.expiration.cancelled");
    private static readonly Counter<long> ExpiredOrdersSkipped =
        Meter.CreateCounter<long>("sales.orders.expiration.skipped");
    private static readonly Counter<long> ExpiredOrdersFailed =
        Meter.CreateCounter<long>("sales.orders.expiration.failed");
    private static readonly Histogram<double> ExpiredOrdersDuration =
        Meter.CreateHistogram<double>("sales.orders.expiration.duration", unit: "ms");

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

    /// <summary>Counts previously failed inbound messages re-driven to success.</summary>
    public static Counter<long> InboxRetried => Inbox.Retried;

    /// <summary>Counts inbound messages dead-lettered after exhausting re-drive attempts.</summary>
    public static Counter<long> InboxDeadLettered => Inbox.DeadLettered;

    /// <summary>
    /// Updates the observable gauges reporting the current outbox backlog and dead-letter counts.
    /// </summary>
    /// <param name="backlog">Number of outbox rows not yet successfully published or dead-lettered.</param>
    /// <param name="deadLetters">Number of outbox rows currently dead-lettered.</param>
    public static void SetOutboxSnapshot(long backlog, long deadLetters)
    {
        Outbox.SetSnapshot(backlog, deadLetters);
    }

    /// <summary>
    /// Records the outcome of one expired-order cancellation batch.
    /// </summary>
    /// <param name="scanned">Number of expired candidate orders scanned.</param>
    /// <param name="cancelled">Number of orders cancelled.</param>
    /// <param name="skipped">Number of candidates skipped because they were no longer cancellable.</param>
    /// <param name="failed">Number of candidates that failed to cancel.</param>
    /// <param name="elapsedMilliseconds">Wall-clock duration of the batch, in milliseconds.</param>
    public static void RecordExpiredOrderCancellation(
        long scanned,
        long cancelled,
        long skipped,
        long failed,
        double elapsedMilliseconds)
    {
        ExpiredOrdersScanned.Add(scanned);
        ExpiredOrdersCancelled.Add(cancelled);
        ExpiredOrdersSkipped.Add(skipped);
        ExpiredOrdersFailed.Add(failed);
        ExpiredOrdersDuration.Record(elapsedMilliseconds);
    }
}
