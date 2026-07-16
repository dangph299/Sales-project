namespace BuildingBlocks.Contracts;

/// <summary>
/// Canonical audit event contract consumed by AuditLog and shared by every bounded context.
/// </summary>
public sealed record AuditLogEvent
{
    /// <summary>
    /// Gets the stable audit identifier used for deduplication.
    /// </summary>
    public required Guid AuditId { get; init; }

    /// <summary>
    /// Gets the service that produced the audit event.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Gets the event type for consumers and diagnostics.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Gets the audited aggregate/entity type.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the audited aggregate/entity identifier.
    /// </summary>
    public required string EntityId { get; init; }

    /// <summary>
    /// Gets the audit action.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets a business-readable description when a generic diff is not enough.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the actor identifier.
    /// </summary>
    public string? ActorId { get; init; }

    /// <summary>
    /// Gets the actor display name.
    /// </summary>
    public string? ActorName { get; init; }

    /// <summary>
    /// Gets the correlation identifier as text.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the causation identifier as text.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Gets the distributed trace identifier.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Gets when the audited operation occurred.
    /// </summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Gets the audit schema version.
    /// </summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>
    /// Gets the field changes captured for this event.
    /// </summary>
    public IReadOnlyCollection<AuditChange> Changes { get; init; } = Array.Empty<AuditChange>();

    /// <summary>
    /// Gets optional business metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
