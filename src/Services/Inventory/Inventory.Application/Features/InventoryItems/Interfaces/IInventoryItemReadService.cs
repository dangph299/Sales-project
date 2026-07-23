using Inventory.Application.Features.InventoryItems.DTOs;

namespace Inventory.Application.Features.InventoryItems.Interfaces;

/// <summary>
/// Read-side port for inventory item queries.
/// </summary>
public interface IInventoryItemReadService
{
    /// <summary>
    /// Gets a product's inventory snapshot.
    /// </summary>
    Task<InventorySnapshot?> GetAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Aggregates stock-status counts across all tracked inventory items.</summary>
    Task<InventorySummary> GetSummaryAsync(InventorySummaryFilter filter, CancellationToken cancellationToken = default);
}
