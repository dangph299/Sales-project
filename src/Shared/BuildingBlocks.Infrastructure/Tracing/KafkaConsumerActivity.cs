using System.Diagnostics;
using KafkaFlow;
using ContractHeaders = BuildingBlocks.Contracts.MessageHeaders;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Opens the tracing span for a single Kafka-consumed message, shared by the Sales and Inventory
/// consumer handlers so each doesn't keep its own copy of the same trace-context-parsing and
/// tag-setting logic.
/// </summary>
public static class KafkaConsumerActivity
{
    /// <summary>
    /// Parses the W3C trace context carried in the message headers and starts a consumer-kind
    /// activity linked to the producer's trace, tagged with the standard OpenTelemetry messaging
    /// attributes.
    /// </summary>
    /// <param name="source">The <see cref="ActivitySource"/> to start the activity from.</param>
    /// <param name="context">KafkaFlow message context, providing the headers and consumer topic/group.</param>
    /// <returns>Started activity, or <see langword="null"/> if there are no listeners for <paramref name="source"/>.</returns>
    public static Activity? Start(ActivitySource source, IMessageContext context)
    {
        var parentContext = TraceContextParser.Parse(
            context.Headers.GetString(ContractHeaders.TraceParent),
            context.Headers.GetString(ContractHeaders.TraceState));
        var activity = source.StartActivity(
            $"kafka.consume {context.ConsumerContext.Topic}", ActivityKind.Consumer, parentContext);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", context.ConsumerContext.Topic);
        activity?.SetTag("messaging.kafka.consumer.group", context.ConsumerContext.GroupId);
        return activity;
    }
}
