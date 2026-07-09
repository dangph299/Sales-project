namespace Inventory.Application;

/// <summary>
/// Application-facing port for Inventory use cases, called by the Inventory API controllers.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Gets a product's current stock levels.
    /// </summary>
    /// <param name="productId">
    /// The unique identifier of the product to look up.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The product's stock snapshot, or <see langword="null"/> if no inventory item exists for it.
    /// </returns>
    Task<InventorySnapshot?> GetAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the reservation made for a Sales order, if any.
    /// </summary>
    /// <param name="orderId">
    /// The unique identifier of the Sales order to look up.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The reservation snapshot, or <see langword="null"/> if no reservation exists for the order.
    /// </returns>
    Task<ReservationSnapshot?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually adjusts a product's available stock, creating the inventory item if it does not
    /// already exist.
    /// </summary>
    /// <param name="productId">
    /// The unique identifier of the product to adjust.
    /// </param>
    /// <param name="sku">
    /// The product's SKU, used when creating a new inventory item.
    /// </param>
    /// <param name="quantityDelta">
    /// The signed quantity to add to the available stock.
    /// </param>
    /// <param name="actor">
    /// The user or system responsible for the adjustment, recorded for auditing.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The product's stock snapshot after the adjustment.
    /// </returns>
    Task<InventorySnapshot> AdjustAsync(Guid productId, string sku, int quantityDelta, string actor, CancellationToken cancellationToken = default);
}
