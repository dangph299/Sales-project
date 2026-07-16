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
        Consumer = consumer
    };
}
