using System.Diagnostics;
using System.Text.Json;
using BuildingBlocks.Infrastructure;
using KafkaFlow;
using KafkaFlow.Producers;
using Microsoft.Extensions.Logging;
using ContractHeaders = BuildingBlocks.Contracts.MessageHeaders;
using EventEnvelope = BuildingBlocks.Contracts.EventEnvelope;
using KafkaHeaders = KafkaFlow.MessageHeaders;

namespace Inventory.Infrastructure;

/// <summary>
/// Publishes a single outbox row to Kafka, opening a tracing span and propagating the W3C
/// <c>traceparent</c>/<c>tracestate</c> headers so the consumer can continue the same distributed trace.
/// </summary>
public sealed class KafkaOutboxPublisher(IProducerAccessor producers, ILogger<KafkaOutboxPublisher> logger) : IOutboxPublisher
{
    /// <inheritdoc/>
    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Payload)!;

        using var activity = InventoryActivitySource.Instance.StartActivity($"kafka.publish {message.Topic}", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", message.Topic);

        var headers = new KafkaHeaders();
        var traceParent = activity?.Id ?? Activity.Current?.Id;
        if (traceParent is not null) headers.SetString(ContractHeaders.TraceParent, traceParent);
        var traceState = activity?.TraceStateString ?? Activity.Current?.TraceStateString;
        if (!string.IsNullOrEmpty(traceState)) headers.SetString(ContractHeaders.TraceState, traceState);

        var sw = Stopwatch.StartNew();
        var producer = producers.GetProducer("inventory-outbox");
        var report = await producer.ProduceAsync(message.Topic, envelope.AggregateId.ToString(), envelope, headers);

        logger.LogInformation(
            "Published {EventType} {Topic} {Partition} {Offset} {MessageId} {CorrelationId} {AggregateId} {ElapsedMs}",
            envelope.EventType, message.Topic, report.Partition.Value, report.Offset.Value,
            envelope.EventId, envelope.CorrelationId, envelope.AggregateId, sw.ElapsedMilliseconds);
    }
}
