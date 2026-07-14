using MediatR;

namespace Inventory.Application;

/// <summary>
/// Handles manual stock adjustments. Runs inside a serializable transaction opened by
/// <see cref="InventoryTransactionBehavior{TRequest,TResponse}"/>, so two concurrent adjustments
/// for the same or a not-yet-existing product fail with a persistence conflict instead of silently
/// corrupting stock — the API layer maps that conflict to <c>409 Conflict</c>.
/// </summary>
public sealed class AdjustInventoryCommandHandler(
    IInventoryRepository inventory,
    IInventoryEventOutbox outbox) : IRequestHandler<AdjustInventoryCommand, InventorySnapshot>
{
    /// <inheritdoc/>
    public async Task<InventorySnapshot> Handle(AdjustInventoryCommand request, CancellationToken cancellationToken)
    {
        var item = await inventory.GetByProductIdAsync(request.ProductId, cancellationToken);
        if (item is null)
        {
            item = InventoryItem.Create(request.ProductId, request.Sku, request.QuantityDelta);
            inventory.Add(item);
        }
        else
        {
            item.Adjust(request.QuantityDelta);
        }

        outbox.EnqueueInventoryAdjusted(request.ProductId, item.Version, request.QuantityDelta, item.Available, request.Actor);
        return item.ToSnapshot();
    }
}
