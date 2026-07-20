namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Strategy for publishing a single claimed outbox message to its transport (Kafka).
/// </summary>
public interface IOutboxPublisher
{
    /// <summary>
    /// Publishes a single outbox message.
    /// </summary>
    /// <param name="message">Outbox message.</param>
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
