using System.Text.Json;
using BuildingBlocks.Contracts;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Persistence record for a domain event pending (or having attempted) publication to Kafka,
/// implementing the transactional outbox pattern. Shared by every service's <c>outbox_messages</c>
/// table; each service owns its own <c>IEntityTypeConfiguration&lt;OutboxMessage&gt;</c> mapping.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>
    /// The maximum number of publish attempts before a message is dead-lettered.
    /// </summary>
    public const int MaxAttempts = 10;

    /// <summary>
    /// Gets or sets the unique identifier of this message, matching the wrapped event's <c>EventId</c>.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the Kafka topic this message must be published to.
    /// </summary>
    public string Topic { get; set; } = null!;

    /// <summary>
    /// Gets or sets the serialized <c>EventEnvelope</c> payload.
    /// </summary>
    public string Payload { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UTC instant the underlying event occurred.
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant this message was successfully published, or <see langword="null"/> if not yet published.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant at or after which the next publish attempt may occur.
    /// </summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant this message was dead-lettered after exceeding <see cref="MaxAttempts"/>,
    /// or <see langword="null"/> if it has not been dead-lettered.
    /// </summary>
    public DateTimeOffset? DeadLetteredAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant until which this message is locked by a publisher instance, or
    /// <see langword="null"/> if it is not currently locked.
    /// </summary>
    public DateTimeOffset? LockedUntil { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the publisher batch currently holding the lock, or <see langword="null"/> if unlocked.
    /// </summary>
    public Guid? LockId { get; set; }

    /// <summary>
    /// Gets or sets the number of publish attempts made so far.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the error message from the most recent failed publish attempt, or <see langword="null"/>.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Creates a new, unpublished <see cref="OutboxMessage"/> wrapping the given event envelope.
    /// </summary>
    /// <param name="envelope">
    /// The event envelope to persist as the message payload.
    /// </param>
    /// <param name="topic">
    /// The Kafka topic the message must be published to.
    /// </param>
    /// <returns>
    /// A new <see cref="OutboxMessage"/> ready to be added to the outbox table.
    /// </returns>
    public static OutboxMessage From(EventEnvelope envelope, string topic) => new()
    {
        Id = envelope.EventId,
        Topic = topic,
        Payload = JsonSerializer.Serialize(envelope),
        OccurredAt = envelope.OccurredAt
    };
}
