using System.Diagnostics;
using BuildingBlocks.Contracts;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Builds the standard log-context properties for an event envelope.
/// </summary>
public static class EventEnvelopeLogContext
{
    public static MessageLogContextProperty[] From(EventEnvelope envelope, Activity? activity = null) =>
    [
        new("EventId", envelope.EventId),
        new("EventType", envelope.EventType),
        new("CorrelationId", envelope.CorrelationId),
        new("TraceId", activity?.TraceId.ToHexString())
    ];
}
