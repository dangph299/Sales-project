namespace Dashboard.Bff.Options;

/// <summary>
/// Inventory-related settings used when aggregating the dashboard snapshot.
/// </summary>
public sealed class DashboardInventoryOptions
{
    public const string SectionName = "Dashboard:Inventory";

    /// <summary>Quantity at or below which an inventory item is considered low stock.</summary>
    public int LowStockThreshold { get; set; } = 5;
}
