using System.Diagnostics.Metrics;

namespace BuildingBlocks.Infrastructure.Observability.Metrics;

/// <summary>
/// Shared OpenTelemetry instruments for a service's outbox publish pipeline. Each service
/// constructs its own instance with its own <see cref="Meter"/> name and metric-name prefix, so
/// the resulting instrument names are unchanged from before this type existed.
/// </summary>
public sealed class OutboxMetrics
{
    private long _backlog;
    private long _deadLetters;
    private long _oldestPendingAgeSeconds;
    private long _failedTerminal;

    /// <summary>
    /// Creates the outbox counters and gauges for a service, scoped to the given meter and
    /// metric-name prefix.
    /// </summary>
    /// <param name="meterName">Name of the <see cref="Meter"/> to create the instruments on. Must match the name passed to the service's <c>AddMeter(...)</c> registration for the instruments to be exported.</param>
    /// <param name="prefix">Metric-name prefix (e.g. <c>"sales"</c> or <c>"inventory"</c>) prepended to each instrument name.</param>
    public OutboxMetrics(string meterName, string prefix)
    {
        var meter = new Meter(meterName);

        Published = meter.CreateCounter<long>($"{prefix}.outbox.published");
        Failed = meter.CreateCounter<long>($"{prefix}.outbox.failed");
        DeadLettered = meter.CreateCounter<long>($"{prefix}.outbox.deadlettered");
        RetryRequested = meter.CreateCounter<long>($"{prefix}.outbox.retry_requested");

        meter.CreateObservableGauge($"{prefix}.outbox.backlog", () => Interlocked.Read(ref _backlog));
        meter.CreateObservableGauge($"{prefix}.outbox.deadletters", () => Interlocked.Read(ref _deadLetters));
        meter.CreateObservableGauge($"{prefix}.outbox.oldest_pending_age_seconds", () => Interlocked.Read(ref _oldestPendingAgeSeconds));
        meter.CreateObservableGauge($"{prefix}.outbox.failed_terminal", () => Interlocked.Read(ref _failedTerminal));
    }

    /// <summary>Counts outbox rows successfully published to Kafka.</summary>
    public Counter<long> Published { get; }

    /// <summary>Counts outbox publish attempts that failed.</summary>
    public Counter<long> Failed { get; }

    /// <summary>Counts outbox rows that exceeded their maximum publish attempts and were dead-lettered.</summary>
    public Counter<long> DeadLettered { get; }

    /// <summary>Counts terminal failed outbox rows reset for publisher retry by maintenance jobs.</summary>
    public Counter<long> RetryRequested { get; }

    /// <summary>
    /// Updates the observable gauges reporting the current outbox backlog and dead-letter counts.
    /// </summary>
    /// <param name="backlog">Number of outbox rows not yet successfully published or dead-lettered.</param>
    /// <param name="deadLetters">Number of outbox rows currently dead-lettered.</param>
    public void SetSnapshot(long backlog, long deadLetters)
    {
        Interlocked.Exchange(ref _backlog, backlog);
        Interlocked.Exchange(ref _deadLetters, deadLetters);
    }

    /// <summary>
    /// Updates observable gauges reported by the outbox pending monitor.
    /// </summary>
    public void SetPendingSnapshot(long backlog, long oldestPendingAgeSeconds, long failedTerminal)
    {
        Interlocked.Exchange(ref _backlog, backlog);
        Interlocked.Exchange(ref _oldestPendingAgeSeconds, oldestPendingAgeSeconds);
        Interlocked.Exchange(ref _failedTerminal, failedTerminal);
        Interlocked.Exchange(ref _deadLetters, failedTerminal);
    }
}
