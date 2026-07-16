namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Shared options for inbound Kafka message failure handling.
/// </summary>
public sealed class InboxConsumerOptions
{
    /// <summary>
    /// Maximum number of failed attempts before an inbound message is marked dead-lettered.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;
}
