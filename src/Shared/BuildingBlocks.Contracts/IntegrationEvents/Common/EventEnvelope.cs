using System.Text.Json;

namespace BuildingBlocks.Contracts;

/// <summary>
/// The transport envelope wrapping every event published to Kafka: pure transport data, not a
/// payload itself. See <c>EventEnvelopeFactory</c> in <c>BuildingBlocks.Infrastructure</c> for
/// how instances are constructed.
/// </summary>
/// <param name="EventId">
/// The unique identifier of this event, used for Inbox/audit deduplication.
/// </param>
/// <param name="EventType">
/// The event's type name, used by consumers to select how to deserialize/handle <see cref="Data"/>.
/// </param>
/// <param name="AggregateId">
/// The unique identifier of the aggregate this event relates to.
/// </param>
/// <param name="Version">
/// The aggregate's version when the event was raised.
/// </param>
/// <param name="CorrelationId">
/// The correlation identifier used to trace a business workflow across services.
/// </param>
/// <param name="CausationId">
/// The identifier of the event that caused this one, if any.
/// </param>
/// <param name="OccurredAt">
/// The UTC instant the event occurred.
/// </param>
/// <param name="Actor">
/// The user or system responsible for the operation that raised the event.
/// </param>
/// <param name="Data">
/// The serialized event payload.
/// </param>
public sealed record EventEnvelope(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    long Version,
    Guid CorrelationId,
    Guid? CausationId,
    DateTimeOffset OccurredAt,
    string Actor,
    JsonElement Data);
