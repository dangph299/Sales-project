using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuditLog.Infrastructure;

/// <summary>
/// MongoDB document storing one audit event. Upserted keyed by <see cref="AuditId"/> so
/// replays/redeliveries do not create duplicate documents.
/// </summary>
public sealed class AuditDocument
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    /// <summary>
    /// Gets or sets the unique audit identifier.
    /// </summary>
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid AuditId { get; set; }

    /// <summary>
    /// Gets or sets the transport event identifier.
    /// </summary>
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the service that produced the audit event.
    /// </summary>
    public string ServiceName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the audit event type.
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the audited entity type.
    /// </summary>
    public string EntityType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the audited entity identifier.
    /// </summary>
    public string EntityId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the audit action.
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// Gets or sets an optional business description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the transport aggregate the event relates to.
    /// </summary>
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the aggregate's version when the event was raised.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier used to trace the request across services.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the event that caused this one, if any.
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Gets or sets the trace identifier.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant the event occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>
    /// Gets or sets the actor identifier.
    /// </summary>
    public string? ActorId { get; set; }

    /// <summary>
    /// Gets or sets the actor display name.
    /// </summary>
    public string? ActorName { get; set; }

    /// <summary>
    /// Gets or sets the legacy actor from the transport envelope.
    /// </summary>
    public string Actor { get; set; } = null!;

    /// <summary>
    /// Gets or sets the audit schema version.
    /// </summary>
    public int SchemaVersion { get; set; }

    /// <summary>
    /// Gets or sets the field-level changes.
    /// </summary>
    public IReadOnlyCollection<AuditChangeDocument> Changes { get; set; } = [];

    /// <summary>
    /// Gets or sets optional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets or sets the event's raw JSON payload for diagnostics.
    /// </summary>
    public string Payload { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Kafka topic the event was consumed from.
    /// </summary>
    public string Topic { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Kafka partition the event was consumed from.
    /// </summary>
    public int Partition { get; set; }

    /// <summary>
    /// Gets or sets the Kafka offset the event was consumed from.
    /// </summary>
    public long Offset { get; set; }

    /// <summary>
    /// Gets or sets when AuditLog received the event.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; set; }
}

/// <summary>
/// MongoDB representation of one field-level audit change.
/// </summary>
public sealed class AuditChangeDocument
{
    /// <summary>
    /// Gets or sets the property path.
    /// </summary>
    public string PropertyPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the old value.
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new value.
    /// </summary>
    public object? NewValue { get; set; }
}
