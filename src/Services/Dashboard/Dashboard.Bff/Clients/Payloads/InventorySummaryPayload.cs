namespace Dashboard.Bff.Clients.Payloads;

/// <summary>
/// Shape of the payload returned by Inventory.Api's <c>GET /api/inventory/summary</c> endpoint.
/// </summary>
public sealed record InventorySummaryPayload(
    int TotalItems,
    long TotalQuantity,
    int InStock,
    int LowStock,
    int OutOfStock,
    int LowStockThreshold);
