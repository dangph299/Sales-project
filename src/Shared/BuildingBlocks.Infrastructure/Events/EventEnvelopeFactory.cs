using System.Text.Json;
using BuildingBlocks.Contracts;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Builds <see cref="EventEnvelope"/> instances for outgoing Kafka messages, serializing the
/// payload and filling in transport metadata.
/// </summary>
public static class EventEnvelopeFactory
{
    /// <summary>
    /// Creates an <see cref="EventEnvelope"/> wrapping a serialized event payload.
    /// </summary>
    /// <param name="aggregateId">Aggregate identifier.</param>
    /// <param name="version">Aggregate version when the event was raised.</param>
    /// <param name="data">Event payload.</param>
    /// <param name="actor">User or system performing the operation. Defaults to <c>"system"</c>.</param>
    /// <param name="correlationId">Correlation identifier. A new one is generated if not supplied.</param>
    /// <param name="causationId">Causation identifier.</param>
    /// <returns>Envelope ready for publication.</returns>
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
