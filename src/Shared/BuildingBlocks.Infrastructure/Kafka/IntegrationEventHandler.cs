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
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await Process(envelope);
                logger.LogInformation(
                    "Consumed {EventType} {Topic} {GroupId} {Partition} {Offset} {MessageId} {AggregateId} {OrderId} {Result} {ElapsedMs}",
                    envelope.EventType, context.ConsumerContext.Topic, context.ConsumerContext.GroupId,
                    context.ConsumerContext.Partition, context.ConsumerContext.Offset, envelope.EventId,
                    envelope.AggregateId, envelope.AggregateId, result, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                var failure = await RecordFailure(context, envelope, ex);
                logger.LogError(ex,
                    "Consume failed {EventType} {Topic} {GroupId} {Partition} {Offset} {MessageId} {Attempts} {DeadLettered} {ElapsedMs}",
                    envelope.EventType, context.ConsumerContext.Topic, context.ConsumerContext.GroupId,
                    context.ConsumerContext.Partition, context.ConsumerContext.Offset, envelope.EventId,
                    failure?.Attempts, failure?.DeadLettered, stopwatch.ElapsedMilliseconds);

                // No failure recorder means we have nowhere to persist the event for retry, so rethrow
                // to surface the loss loudly rather than swallow it silently.
                if (failure is null)
                {
                    throw;
                }

                if (failure.DeadLettered)
                {
                    logger.LogError(
                        "Inbound message dead-lettered {EventType} {Topic} {GroupId} {Partition} {Offset} {MessageId} {Attempts}",
                        envelope.EventType, context.ConsumerContext.Topic, context.ConsumerContext.GroupId,
                        context.ConsumerContext.Partition, context.ConsumerContext.Offset, envelope.EventId,
                        failure.Attempts);
                }

                // Do not rethrow: KafkaFlow commits the offset regardless, so a throw would drop the
                // event rather than retry it. The failure is now durably recorded in the inbox and the
                // InboxRedriveService owns retry with exponential backoff and dead-lettering.
            }
        }
    }

    private async Task<string> Process(EventEnvelope envelope)
    {
        await using var scope = scopes.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IIntegrationEventProcessor>();
        return await processor.ProcessAsync(envelope);
    }

    private async Task<InboundFailureResult?> RecordFailure(
        IMessageContext context,
        EventEnvelope envelope,
        Exception exception)
    {
        await using var scope = scopes.CreateAsyncScope();
        var recorder = scope.ServiceProvider.GetService<IInboxFailureRecorder>();
        if (recorder is null) return null;

        return await recorder.RecordFailureAsync(
            envelope,
            new InboundMessageContext(
                context.ConsumerContext.Topic,
                context.ConsumerContext.GroupId,
                context.ConsumerContext.Partition,
                context.ConsumerContext.Offset),
            exception);
    }
}
