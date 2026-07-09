using System.Diagnostics;

namespace BuildingBlocks.Contracts;

/// <summary>
/// Parses the W3C traceparent/tracestate values every Kafka consumer handler extracts from
/// message headers to restore the producer's distributed-trace context. Shared so the three
/// services' consumer handlers don't each keep their own copy of the same parsing logic.
/// </summary>
public static class TraceContextParser
{
    /// <summary>
    /// Parses W3C <c>traceparent</c>/<c>tracestate</c> header values into an <see cref="ActivityContext"/>.
    /// </summary>
    /// <param name="traceParent">
    /// The <c>traceparent</c> header value, or <see langword="null"/>/empty if the message carries no trace context.
    /// </param>
    /// <param name="traceState">
    /// The <c>tracestate</c> header value, if any.
    /// </param>
    /// <returns>
    /// The parsed activity context, or <c>default</c> if <paramref name="traceParent"/> is missing or invalid.
    /// </returns>
    public static ActivityContext Parse(string? traceParent, string? traceState)
    {
        if (string.IsNullOrEmpty(traceParent)) return default;
        return ActivityContext.TryParse(traceParent, traceState, out var parsed) ? parsed : default;
    }
}
