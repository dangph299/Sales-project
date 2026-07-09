namespace Sales.Infrastructure;

/// <summary>
/// Persistence record marking a Kafka message as processed. <see cref="EventId"/> is the primary
/// key, so a unique-constraint violation on insert means the message was already handled.
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
    /// Gets or sets an identifier for the consumer that processed this event.
    /// </summary>
    public string Consumer { get; set; } = null!;
}
