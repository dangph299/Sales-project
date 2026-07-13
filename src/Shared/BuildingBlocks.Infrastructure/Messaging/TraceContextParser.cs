using System.Diagnostics;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Parses W3C trace context values carried by transported messages.
/// </summary>
public static class TraceContextParser
{
    public static ActivityContext Parse(string? traceParent, string? traceState)
    {
        if (string.IsNullOrEmpty(traceParent))
        {
            return default;
        }

        return ActivityContext.TryParse(traceParent, traceState, out var parsed) ? parsed : default;
    }
}
