namespace Inventory.Application.Features.InventoryItems.DTOs;

/// <summary>Aggregated stock-status counts across tracked inventory items.</summary>
public sealed record InventorySummary(
    int TotalItems,
    long TotalQuantity,
    int InStock,
    int LowStock,
    int OutOfStock,
    int LowStockThreshold);
