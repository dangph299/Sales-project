using System.Diagnostics;
using BuildingBlocks.Contracts;
using KafkaFlow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Base KafkaFlow handler for integration events. It owns tracing, message log context, structured
/// consume logs, and scoped processor resolution.
/// </summary>
public abstract class IntegrationEventHandler<THandler>(
    IServiceScopeFactory scopes,
    ILogger<THandler> logger,
    ActivitySource activitySource,
    IMessageLogContext messageLogContext) : IMessageHandler<EventEnvelope>
{
    /// <inheritdoc />
    public async Task Handle(IMessageContext context, EventEnvelope envelope)
    {
        using var activity = KafkaConsumerActivity.Start(activitySource, context);

        using (messageLogContext.Push(EventEnvelopeLogContext.From(envelope, activity)))
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await Process(envelope);
                logger.LogInformation(
                    "Consumed {EventType} {Topic} {GroupId} {Partition} {Offset} {MessageId} {AggregateId} {OrderId} {Result} {ElapsedMs}",
                    envelope.EventType, context.ConsumerContext.Topic, context.ConsumerContext.GroupId,
                    context.ConsumerContext.Partition, context.ConsumerContext.Offset, envelope.EventId,
                    envelope.AggregateId, envelope.AggregateId, result, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Consume failed {EventType} {Topic} {GroupId} {Partition} {Offset} {MessageId} {ElapsedMs}",
                    envelope.EventType, context.ConsumerContext.Topic, context.ConsumerContext.GroupId,
                    context.ConsumerContext.Partition, context.ConsumerContext.Offset, envelope.EventId, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private async Task<string> Process(EventEnvelope envelope)
    {
        await using var scope = scopes.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IIntegrationEventProcessor>();
        return await processor.ProcessAsync(envelope);
    }
}
