using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuditLog.Infrastructure;

/// <summary>
/// MongoDB document storing one Kafka event for audit purposes. Upserted keyed by
/// <see cref="EventId"/> so replays/redeliveries do not create duplicate documents.
/// </summary>
public sealed class AuditDocument
{
    /// <summary>
    /// Gets or sets the MongoDB document identifier.
    /// </summary>
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    /// <summary>
    /// Gets or sets the unique identifier of the audited event.
    /// </summary>
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the event's type name.
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the aggregate the event relates to.
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
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the event that caused this one, if any.
    /// </summary>
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid? CausationId { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant the event occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>
    /// Gets or sets the user or system responsible for the event.
    /// </summary>
    public string Actor { get; set; } = null!;

    /// <summary>
    /// Gets or sets the event's raw JSON payload.
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
}
