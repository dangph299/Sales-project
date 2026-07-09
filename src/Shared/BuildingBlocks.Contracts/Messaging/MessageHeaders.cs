namespace BuildingBlocks.Contracts;

/// <summary>
/// Kafka message header names used across services, centralized here to avoid magic strings.
/// </summary>
public static class MessageHeaders
{
    /// <summary>Header carrying the contract version, see <see cref="ContractVersions"/>.</summary>
    public const string ContractVersion = "contract-version";

    /// <summary>Header carrying the event's correlation identifier.</summary>
    public const string CorrelationId = "correlation-id";

    /// <summary>Header carrying the identifier of the event that caused this one.</summary>
    public const string CausationId = "causation-id";

    /// <summary>Header carrying the event's unique identifier.</summary>
    public const string EventId = "event-id";

    /// <summary>Header carrying the event's type name.</summary>
    public const string EventType = "event-type";

    /// <summary>Header carrying the UTC instant the event occurred.</summary>
    public const string OccurredAt = "occurred-at";

    /// <summary>Header carrying the W3C <c>traceparent</c> value for distributed trace propagation.</summary>
    public const string TraceParent = "traceparent";

    /// <summary>Header carrying the W3C <c>tracestate</c> value for distributed trace propagation.</summary>
    public const string TraceState = "tracestate";
}
