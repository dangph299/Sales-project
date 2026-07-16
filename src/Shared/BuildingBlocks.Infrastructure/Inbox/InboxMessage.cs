namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Persistence record marking a consumed integration event as processed, implementing the inbox
/// idempotency pattern. Shared by every service's <c>inbox_messages</c> table; each service owns its
/// own <c>IEntityTypeConfiguration&lt;InboxMessage&gt;</c> mapping (mirroring <see cref="OutboxMessage"/>).
/// <see cref="EventId"/> is the primary key, so a unique-constraint violation on insert means the
/// message was already handled.
/// </summary>
public sealed class InboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier of the processed event.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant the event was processed.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the processing status for this consumed event.
    /// </summary>
    public InboxMessageStatus Status { get; set; } = InboxMessageStatus.Processed;

    /// <summary>
    /// Gets or sets how many failed processing attempts have been recorded for this event.
    /// Successful duplicate deliveries keep this value unchanged.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant of the most recent failed attempt.
    /// </summary>
    public DateTimeOffset? LastFailedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant this inbound event was dead-lettered after repeated failures.
    /// </summary>
    public DateTimeOffset? DeadLetteredAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant at or after which a failed event becomes eligible for the next
    /// re-drive attempt, or <see langword="null"/> when it is not awaiting a retry.
    /// </summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>
    /// Gets or sets the serialized event envelope, retained for failed events so the inbox re-drive
    /// background service can replay them. <see langword="null"/> for events that succeeded on first
    /// delivery and never needed to be stored for retry.
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Gets or sets the exception type from the most recent failed attempt.
    /// </summary>
    public string? LastExceptionType { get; set; }

    /// <summary>
    /// Gets or sets a shortened error message from the most recent failed attempt.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets the Kafka topic the failed event was consumed from.
    /// </summary>
    public string? OriginalTopic { get; set; }

    /// <summary>
    /// Gets or sets the Kafka partition the failed event was consumed from.
    /// </summary>
    public int? OriginalPartition { get; set; }

    /// <summary>
    /// Gets or sets the Kafka offset the failed event was consumed from.
    /// </summary>
    public long? OriginalOffset { get; set; }

    /// <summary>
    /// Gets or sets the Kafka consumer group that failed to process this event.
    /// </summary>
    public string? OriginalConsumerGroup { get; set; }

    /// <summary>
    /// Gets or sets an optional identifier for the consumer that processed this event, or
    /// <see langword="null"/> when the service does not track it.
    /// </summary>
    public string? Consumer { get; set; }

    /// <summary>
    /// Creates an inbox record for a just-processed event.
    /// </summary>
    /// <param name="eventId">Identifier of the processed event (primary key).</param>
    /// <param name="processedAt">UTC instant the event was processed.</param>
    /// <param name="consumer">Optional identifier of the consumer that processed the event.</param>
    /// <returns>A new <see cref="InboxMessage"/> ready to be added to the inbox table.</returns>
    public static InboxMessage Create(Guid eventId, DateTimeOffset processedAt, string? consumer = null) => new()
    {
        EventId = eventId,
        ProcessedAt = processedAt,
        Status = InboxMessageStatus.Processed,
        Consumer = consumer
    };
}
