using System.Text.Json;
using BuildingBlocks.Contracts;

namespace Inventory.Infrastructure;

/// <summary>
/// Builds <see cref="EventEnvelope"/> instances for outgoing Kafka messages, serializing the
/// payload and filling in transport metadata.
/// </summary>
internal static class EventEnvelopeFactory
{
    /// <summary>
    /// Creates an <see cref="EventEnvelope"/> wrapping a serialized event payload.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the event payload.
    /// </typeparam>
    /// <param name="aggregateId">
    /// The unique identifier of the aggregate the event relates to.
    /// </param>
    /// <param name="version">
    /// The aggregate's version when the event was raised.
    /// </param>
    /// <param name="data">
    /// The event payload to serialize.
    /// </param>
    /// <param name="actor">
    /// The user or system responsible for the operation. Defaults to <c>"system"</c>.
    /// </param>
    /// <param name="correlationId">
    /// The correlation identifier used to trace the request across services. A new one is
    /// generated if not supplied.
    /// </param>
    /// <param name="causationId">
    /// The identifier of the event that caused this one, if any.
    /// </param>
    /// <returns>
    /// The populated envelope, ready to be persisted to the outbox and published.
    /// </returns>
    public static EventEnvelope Create<T>(
        Guid aggregateId,
        long version,
        T data,
        string actor = "system",
        Guid? correlationId = null,
        Guid? causationId = null)
    {
        var eventType = data?.GetType().Name ?? typeof(T).Name;
        var payload = data is null
            ? JsonSerializer.SerializeToElement(data)
            : JsonSerializer.SerializeToElement(data, data.GetType());

        return new(
            Guid.NewGuid(),
            eventType,
            aggregateId,
            version,
            correlationId ?? Guid.NewGuid(),
            causationId,
            DateTimeOffset.UtcNow,
            actor,
            payload);
    }
}
