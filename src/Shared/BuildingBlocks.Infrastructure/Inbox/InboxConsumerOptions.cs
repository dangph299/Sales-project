namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Shared options for inbound Kafka message failure handling and re-drive.
/// </summary>
public sealed class InboxConsumerOptions
{
    /// <summary>
    /// Maximum number of failed attempts before an inbound message is marked dead-lettered.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Seconds between inbox re-drive polling cycles.
    /// </summary>
    public int RedrivePollSeconds { get; set; } = 15;

    /// <summary>
    /// Maximum number of failed inbox rows re-driven per cycle.
    /// </summary>
    public int RedriveBatchSize { get; set; } = 50;

    /// <summary>
    /// Gets the clamped polling interval between inbox re-drive cycles.
    /// </summary>
    public TimeSpan RedrivePollInterval => TimeSpan.FromSeconds(Math.Clamp(RedrivePollSeconds, 1, 3600));
}
