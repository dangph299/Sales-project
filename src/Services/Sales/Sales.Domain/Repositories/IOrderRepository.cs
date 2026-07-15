namespace Sales.Domain;

/// <summary>
/// Command-side persistence contract for <see cref="Order"/>, adding the eager-loaded lookup used
/// when a command needs the order together with its lines.
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
    /// <summary>
    /// Finds open orders whose last update is older than the expiration cutoff and can be cancelled.
    /// </summary>
    /// <param name="orderUpdatedBefore">Latest allowed order update time.</param>
    /// <param name="batchSize">Maximum number of order identifiers to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Order identifiers ordered by oldest update first.</returns>
    Task<IReadOnlyCollection<Guid>> FindExpiredCancellableOrderIdsAsync(
        DateTimeOffset orderUpdatedBefore,
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single order together with its lines.
    /// </summary>
    /// <param name="id">Order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Order with its lines populated, or <see langword="null"/> if no order with that identifier exists.</returns>
    Task<Order?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
}
