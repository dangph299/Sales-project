namespace Dashboard.Bff.Contracts;

/// <summary>
/// Aggregated stock-status counts across tracked inventory items.
/// </summary>
/// <param name="TotalItems">Total number of distinct inventory items.</param>
/// <param name="TotalQuantity">Total quantity on hand across all items.</param>
/// <param name="InStock">Number of items considered in stock.</param>
/// <param name="LowStock">Number of items at or below the low-stock threshold.</param>
/// <param name="OutOfStock">Number of items with zero quantity on hand.</param>
/// <param name="LowStockThreshold">Threshold used to classify items as low stock.</param>
public sealed record InventorySummaryDto(
    int TotalItems,
    long TotalQuantity,
    int InStock,
    int LowStock,
    int OutOfStock,
    int LowStockThreshold);
