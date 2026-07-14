namespace Inventory.Application;

/// <summary>
/// Application port for enqueueing Inventory integration events.
/// </summary>
public interface IInventoryEventOutbox
{
    /// <summary>
    /// Enqueues an audit event after manual stock adjustment.
    /// </summary>
    void EnqueueInventoryAdjusted(Guid productId, long version, int quantityDelta, int available, string actor);

    /// <summary>
    /// Enqueues a successful stock reservation reply.
    /// </summary>
    void EnqueueStockReserved(Guid orderId, long orderVersion, Guid correlationId, Guid causationId);

    /// <summary>
    /// Enqueues a rejected stock reservation reply.
    /// </summary>
    void EnqueueStockRejected(Guid orderId, long orderVersion, string reason, Guid correlationId, Guid causationId);

    /// <summary>
    /// Enqueues a stock release reply.
    /// </summary>
    void EnqueueStockReleased(Guid orderId, long orderVersion, Guid correlationId, Guid causationId);
}
