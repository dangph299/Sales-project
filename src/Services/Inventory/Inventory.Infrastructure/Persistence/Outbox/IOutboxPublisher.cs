namespace Inventory.Infrastructure;

/// <summary>
/// Strategy for publishing a single claimed outbox row to its transport (Kafka).
/// </summary>
public interface IOutboxPublisher
{
    /// <summary>
    /// Publishes a single outbox row.
    /// </summary>
    /// <param name="message">
    /// The outbox row to publish.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    Task PublishAsync(OutboxRow message, CancellationToken cancellationToken = default);
}
