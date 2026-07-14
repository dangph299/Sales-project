using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Inventory.Application;

namespace Inventory.Infrastructure;

/// <summary>
/// Inventory integration-event outbox adapter.
/// </summary>
public sealed class InventoryEventOutbox(InventoryDbContext db) : IInventoryEventOutbox
{
    /// <inheritdoc/>
    public void EnqueueInventoryAdjusted(Guid productId, long version, int quantityDelta, int available, string actor)
    {
        db.Enqueue(EventEnvelopeFactory.Create(productId, version, new AuditChanged("InventoryItem", productId.ToString(), "Adjusted",
            [
                AuditChangeDetector.Change("QuantityDelta", null, quantityDelta, "Quantity Delta"),
                AuditChangeDetector.Change("Available", null, available, "Available")
            ]), actor), KafkaTopics.InventoryAudit);
    }

    /// <inheritdoc/>
    public void EnqueueStockReserved(Guid orderId, long orderVersion, Guid correlationId, Guid causationId)
    {
        db.Enqueue(
            EventEnvelopeFactory.Create(orderId, orderVersion, new StockReserved(orderId), "inventory", correlationId, causationId),
            KafkaTopics.StockReserved);
    }

    /// <inheritdoc/>
    public void EnqueueStockRejected(Guid orderId, long orderVersion, string reason, Guid correlationId, Guid causationId)
    {
        db.Enqueue(
            EventEnvelopeFactory.Create(orderId, orderVersion, new StockRejected(orderId, reason), "inventory", correlationId, causationId),
            KafkaTopics.StockRejected);
    }

    /// <inheritdoc/>
    public void EnqueueStockReleased(Guid orderId, long orderVersion, Guid correlationId, Guid causationId)
    {
        db.Enqueue(
            EventEnvelopeFactory.Create(orderId, orderVersion, new StockReleased(orderId), "inventory", correlationId, causationId),
            KafkaTopics.StockReleased);
    }
}
