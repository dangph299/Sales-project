namespace Dashboard.Bff.Contracts;

/// <summary>
/// Aggregated dashboard data returned to the Sales web client.
/// </summary>
/// <param name="Metrics">Top-level headline metrics.</param>
/// <param name="Inventory">Aggregated inventory stock-status counts.</param>
/// <param name="RecentOrders">Most recent orders.</param>
/// <param name="OrderChart">Order data points used to render the order chart.</param>
/// <param name="RefreshedAt">Timestamp at which the snapshot was produced.</param>
public sealed record DashboardSnapshot(
    DashboardMetrics Metrics,
    InventorySummaryDto Inventory,
    IReadOnlyList<RecentOrderDto> RecentOrders,
    IReadOnlyList<OrderChartPointDto> OrderChart,
    DateTimeOffset RefreshedAt);
