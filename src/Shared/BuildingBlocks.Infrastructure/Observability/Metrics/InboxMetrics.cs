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
        Retried = meter.CreateCounter<long>($"{prefix}.inbox.retried");
        DeadLettered = meter.CreateCounter<long>($"{prefix}.inbox.dead_lettered");
        CleanupDeleted = meter.CreateCounter<long>($"{prefix}.inbox.cleanup_deleted");
        DeadLetterReplayRequested = meter.CreateCounter<long>($"{prefix}.inbox.dead_letter_replay_requested");
    }

    /// <summary>Counts inbound Kafka messages skipped because they were already recorded in the Inbox.</summary>
    public Counter<long> Duplicate { get; }

    /// <summary>Counts inbound Kafka messages processed successfully for the first time.</summary>
    public Counter<long> Processed { get; }

    /// <summary>Counts previously failed inbound messages re-driven to success by the re-drive service.</summary>
    public Counter<long> Retried { get; }

    /// <summary>Counts inbound messages moved to dead-letter state after exhausting re-drive attempts.</summary>
    public Counter<long> DeadLettered { get; }

    /// <summary>Counts processed inbox rows deleted by retention cleanup.</summary>
    public Counter<long> CleanupDeleted { get; }

    /// <summary>Counts dead-lettered inbox rows reset for re-drive by maintenance jobs.</summary>
    public Counter<long> DeadLetterReplayRequested { get; }
}
