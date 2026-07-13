using System.Text.Json;

namespace BuildingBlocks.Contracts;

/// <summary>
/// transport envelope wrapping every event published to Kafka: pure transport data, not a
/// payload itself. See <c>EventEnvelopeFactory</c> in each service's Infrastructure project for
/// how instances are constructed.
/// </summary>
/// <param name="EventId">This event, used for Inbox/audit deduplication.</param>
/// <param name="EventType">Event's type name, used by consumers to select how to deserialize/handle <see cref="Data"/>.</param>
/// <param name="AggregateId">Aggregate this event relates to.</param>
/// <param name="Version">Aggregate version when the event was raised.</param>
/// <param name="CorrelationId">Correlation identifier used to trace a business workflow across services.</param>
/// <param name="CausationId">Causation identifier.</param>
/// <param name="OccurredAt">UTC instant the event occurred.</param>
/// <param name="Actor">User or system responsible for the operation that raised the event.</param>
/// <param name="Data">Serialized event payload.</param>
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
