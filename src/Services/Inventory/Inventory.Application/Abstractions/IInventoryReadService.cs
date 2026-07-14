namespace Inventory.Application;

/// <summary>
/// Read-side port for inventory queries.
/// </summary>
public interface IInventoryReadService
{
    /// <summary>
    /// Gets a product's inventory snapshot.
    /// </summary>
    Task<InventorySnapshot?> GetAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the reservation for a Sales order.
    /// </summary>
    Task<ReservationSnapshot?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken = default);
}
