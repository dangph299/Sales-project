namespace Inventory.Infrastructure;

/// <summary>
/// Persistence record for an event pending (or having attempted) publication to Kafka,
/// implementing the transactional outbox pattern.
/// </summary>
public sealed class OutboxRow
{
    /// <summary>
    /// The maximum number of publish attempts before a message is dead-lettered.
    /// </summary>
    public const int MaxAttempts = 10;

    /// <summary>
    /// Gets or sets the unique identifier of this row, matching the wrapped event's <c>EventId</c>.
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
    /// Gets or sets the UTC instant until which this row is locked by a publisher instance, or
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
}
