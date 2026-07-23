namespace Inventory.Application.Features.InventoryItems.DTOs;

/// <summary>
/// Inputs for the inventory summary aggregation. Only <paramref name="LowStockThreshold"/> is
/// honored today; the reserved fields are accepted and ignored so future filtering (by warehouse,
/// location, company) can be added without changing this signature or breaking callers.
/// </summary>
public sealed record InventorySummaryFilter(
    int LowStockThreshold,
    Guid? WarehouseId = null,
    Guid? LocationId = null,
    Guid? CompanyId = null);
