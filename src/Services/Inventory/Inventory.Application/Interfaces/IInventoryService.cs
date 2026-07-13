namespace Inventory.Application;

/// <summary>
/// Application-facing port for Inventory use cases, called by the Inventory API controllers.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Gets a product's current stock levels.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Product's stock snapshot, or <see langword="null"/> if no inventory item exists for it.</returns>
    Task<InventorySnapshot?> GetAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the reservation made for a Sales order, if any.
    /// </summary>
    /// <param name="orderId">Sales order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Reservation snapshot, or <see langword="null"/> if no reservation exists for the order.</returns>
    Task<ReservationSnapshot?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually adjusts a product's available stock, creating the inventory item if it does not
    /// already exist.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="sku">Product's SKU, used when creating a new inventory item.</param>
    /// <param name="quantityDelta">Signed quantity to add to the available stock.</param>
    /// <param name="actor">User or system responsible for the adjustment, recorded for auditing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Product's stock snapshot after the adjustment.</returns>
    Task<InventorySnapshot> AdjustAsync(Guid productId, string sku, int quantityDelta, string actor, CancellationToken cancellationToken = default);
}
