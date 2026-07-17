using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Inventory.Application;
using Inventory.Application.Common.Interfaces;

namespace Inventory.Infrastructure;

/// <summary>
/// Inventory integration-event outbox adapter.
/// </summary>
public sealed class InventoryEventOutbox(InventoryDbContext db) : IInventoryEventOutbox
{
    /// <inheritdoc/>
    public void EnqueueInventoryAdjusted(Guid productId, long version, int quantityDelta, int available, string actor)
    {
        var auditEvent = new AuditLogEvent
        {
            AuditId = Guid.NewGuid(),
            ServiceName = "Inventory",
            EventType = "InventoryItemAdjusted",
            EntityType = "InventoryItem",
            EntityId = productId.ToString(),
            Action = "Adjusted",
            Description = "Inventory stock was manually adjusted.",
            ActorId = actor,
            ActorName = actor,
            OccurredAt = DateTimeOffset.UtcNow,
            Changes =
            [
                new AuditChange { PropertyPath = "QuantityDelta", OldValue = null, NewValue = quantityDelta },
                new AuditChange { PropertyPath = "Available", OldValue = null, NewValue = available }
            ]
        };
        db.Enqueue(EventEnvelopeFactory.Create(productId, version, auditEvent, actor), KafkaTopics.InventoryAudit);
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
