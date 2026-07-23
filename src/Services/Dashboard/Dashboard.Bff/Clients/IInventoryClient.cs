using Dashboard.Bff.Contracts;

namespace Dashboard.Bff.Clients;

/// <summary>
/// Typed client for reading inventory data from Inventory.Api.
/// </summary>
public interface IInventoryClient
{
    /// <summary>Returns aggregated stock-status counts across tracked inventory items.</summary>
    Task<InventorySummaryDto> GetInventorySummaryAsync(int lowStockThreshold, CancellationToken cancellationToken);
}
