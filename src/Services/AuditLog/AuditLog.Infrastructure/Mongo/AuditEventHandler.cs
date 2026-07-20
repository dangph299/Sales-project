using System.Diagnostics;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using KafkaFlow;
using Microsoft.Extensions.Logging;
using ContractHeaders = BuildingBlocks.Contracts.MessageHeaders;

namespace AuditLog.Infrastructure;

/// <summary>
/// Kafka consumer handler for every audit/integration topic AuditLog subscribes to, upserting each
/// consumed event into MongoDB via <see cref="IAuditWriter"/>.
/// </summary>
public sealed class AuditEventHandler(
    IAuditWriter writer,
    ILogger<AuditEventHandler> logger,
    IMessageLogContext messageLogContext) : IMessageHandler<EventEnvelope>
{
    /// <summary>
    /// Handles a single consumed message: opens a tracing span linked to the producer's trace, and
    /// upserts the event into MongoDB.
    /// </summary>
    /// <param name="context">KafkaFlow message context, providing topic/partition/offset/headers.</param>
    /// <param name="envelope">Deserialized event envelope.</param>
    public async Task Handle(IMessageContext context, EventEnvelope envelope)
    {
        var parentContext = TraceContextParser.Parse(context.Headers.GetString(ContractHeaders.TraceParent), context.Headers.GetString(ContractHeaders.TraceState));
        using var activity = AuditActivitySource.Instance.StartActivity(
            $"kafka.consume {context.ConsumerContext.Topic}", ActivityKind.Consumer, parentContext);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", context.ConsumerContext.Topic);
        activity?.SetTag("messaging.kafka.consumer.group", context.ConsumerContext.GroupId);

        using (messageLogContext.Push(EventEnvelopeLogContext.From(envelope, activity)))
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await writer.UpsertAsync(envelope, context.ConsumerContext.Topic, context.ConsumerContext.Partition, context.ConsumerContext.Offset);
                logger.LogInformation(
                    "Consumed {EventType} {Topic} {GroupId} {Partition} {Offset} {MessageId} {ElapsedMs}",
                    envelope.EventType, context.ConsumerContext.Topic, context.ConsumerContext.GroupId,
                    context.ConsumerContext.Partition, context.ConsumerContext.Offset, envelope.EventId, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Consume failed {EventType} {Topic} {GroupId} {Partition} {Offset} {MessageId} {ElapsedMs}",
                    envelope.EventType, context.ConsumerContext.Topic, context.ConsumerContext.GroupId,
                    context.ConsumerContext.Partition, context.ConsumerContext.Offset, envelope.EventId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
