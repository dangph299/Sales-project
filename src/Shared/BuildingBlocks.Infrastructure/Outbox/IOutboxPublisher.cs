namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Strategy for publishing a single claimed outbox message to its transport (Kafka).
/// </summary>
public interface IOutboxPublisher
{
    /// <summary>
    /// Publishes a single outbox message.
    /// </summary>
    /// <param name="message">
    /// The outbox message to publish.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
