namespace BuildingBlocks.Contracts;

/// <summary>
/// Transport-neutral metadata associated with an integration event.
/// </summary>
public sealed record EventMetadata(
    string EventType,
    string EventVersion,
    string? CorrelationId,
    string? CausationId,
    string? TraceId,
    IReadOnlyDictionary<string, string>? Headers);
