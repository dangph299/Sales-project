using System.Diagnostics;
using System.Text.Json;
using KafkaFlow;
using KafkaFlow.Producers;
using Microsoft.Extensions.Logging;
using ContractHeaders = BuildingBlocks.Contracts.MessageHeaders;
using EventEnvelope = BuildingBlocks.Contracts.EventEnvelope;
using KafkaHeaders = KafkaFlow.MessageHeaders;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Publishes a single outbox message to Kafka, opening a tracing span and propagating the W3C
/// <c>traceparent</c>/<c>tracestate</c> headers so the consumer can continue the same distributed trace.
/// </summary>
/// <param name="producers">KafkaFlow producer accessor used to resolve the named producer to publish through.</param>
/// <param name="logger">Logger used to record a structured entry after each successful publish.</param>
/// <param name="activitySource">The <see cref="System.Diagnostics.ActivitySource"/> the calling service uses to trace its Kafka publish/consume operations.</param>
/// <param name="producerName">Name of the KafkaFlow producer, as registered with <c>AddProducer</c>, to publish through.</param>
public sealed class KafkaOutboxPublisher(
    IProducerAccessor producers,
    ILogger<KafkaOutboxPublisher> logger,
    ActivitySource activitySource,
    string producerName) : IOutboxPublisher
{
    /// <inheritdoc/>
    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Payload)!;

        using var activity = activitySource.StartActivity($"kafka.publish {message.Topic}", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", message.Topic);

        var headers = new KafkaHeaders();
        var traceParent = activity?.Id ?? Activity.Current?.Id;
        if (traceParent is not null) headers.SetString(ContractHeaders.TraceParent, traceParent);
        var traceState = activity?.TraceStateString ?? Activity.Current?.TraceStateString;
        if (!string.IsNullOrEmpty(traceState)) headers.SetString(ContractHeaders.TraceState, traceState);

        var stopwatch = Stopwatch.StartNew();
        var producer = producers.GetProducer(producerName);
        var report = await producer.ProduceAsync(message.Topic, envelope.AggregateId.ToString(), envelope, headers);

        logger.LogInformation(
            "Published {EventType} {Topic} {Partition} {Offset} {MessageId} {CorrelationId} {AggregateId} {ElapsedMs}",
            envelope.EventType, message.Topic, report.Partition.Value, report.Offset.Value,
            envelope.EventId, envelope.CorrelationId, envelope.AggregateId, stopwatch.ElapsedMilliseconds);
    }
}
