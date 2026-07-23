namespace Inventory.Api.Extensions;

/// <summary>
/// Server-side configuration for the inventory summary endpoint, including the
/// fallback low-stock threshold used when a request omits <c>lowStockThreshold</c>.
/// </summary>
public sealed class InventorySummaryOptions
{
    /// <summary>
    /// Configuration section name: <c>Inventory:Summary</c>.
    /// </summary>
    public const string SectionName = "Inventory:Summary";

    /// <summary>
    /// Default low-stock threshold applied when a request does not specify one.
    /// </summary>
    public int LowStockThreshold { get; set; } = 5;
}
