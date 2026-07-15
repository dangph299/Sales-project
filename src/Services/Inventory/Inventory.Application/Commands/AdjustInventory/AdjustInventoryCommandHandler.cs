using MediatR;

namespace Inventory.Application;

/// <summary>
/// Handles manual stock adjustments. Runs inside a serializable transaction opened by
/// <see cref="InventoryTransactionBehavior{TRequest,TResponse}"/>, so two concurrent adjustments
/// for the same or a not-yet-existing product fail with a persistence conflict instead of silently
/// corrupting stock — the API layer maps that conflict to <c>409 Conflict</c>.
/// </summary>
public sealed class AdjustInventoryCommandHandler(
    IInventoryRepository inventoryRepository,
    IInventoryEventOutbox inventoryEventOutbox) : IRequestHandler<AdjustInventoryCommand, InventorySnapshot>
{
    /// <inheritdoc/>
    public async Task<InventorySnapshot> Handle(AdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        var inventoryItem = await inventoryRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
        if (inventoryItem is null)
        {
            inventoryItem = InventoryItem.Create(request.ProductId, request.Sku, request.QuantityDelta);
            inventoryRepository.Add(inventoryItem);
        }
        else
        {
            inventoryItem.Adjust(request.QuantityDelta);
        }

        inventoryEventOutbox.EnqueueInventoryAdjusted(
            request.ProductId,
            inventoryItem.Version,
            request.QuantityDelta,
            inventoryItem.Available,
            request.Actor);
        return inventoryItem.ToSnapshot();
    }
}
