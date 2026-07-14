using System.Diagnostics.Metrics;

namespace BuildingBlocks.Infrastructure.Observability.Metrics;

/// <summary>
/// Shared OpenTelemetry instruments for a service's inbox deduplication pipeline. Each service
/// constructs its own instance with its own <see cref="Meter"/> name and metric-name prefix, so
/// the resulting instrument names are unchanged from before this type existed.
/// </summary>
public sealed class InboxMetrics
{
    /// <summary>
    /// Creates the inbox counters for a service, scoped to the given meter and metric-name
    /// prefix.
    /// </summary>
    /// <param name="meterName">Name of the <see cref="Meter"/> to create the instruments on. Must match the name passed to the service's <c>AddMeter(...)</c> registration for the instruments to be exported.</param>
    /// <param name="prefix">Metric-name prefix (e.g. <c>"sales"</c> or <c>"inventory"</c>) prepended to each instrument name.</param>
    public InboxMetrics(string meterName, string prefix)
    {
        var meter = new Meter(meterName);

        Duplicate = meter.CreateCounter<long>($"{prefix}.inbox.duplicate");
        Processed = meter.CreateCounter<long>($"{prefix}.inbox.processed");
    }

    /// <summary>Counts inbound Kafka messages skipped because they were already recorded in the Inbox.</summary>
    public Counter<long> Duplicate { get; }

    /// <summary>Counts inbound Kafka messages processed successfully for the first time.</summary>
    public Counter<long> Processed { get; }
}
