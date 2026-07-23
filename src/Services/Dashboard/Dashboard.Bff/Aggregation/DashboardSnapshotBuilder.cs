using BuildingBlocks.Application;
using Dashboard.Bff.Clients;
using Dashboard.Bff.Contracts;
using Dashboard.Bff.Options;
using Microsoft.Extensions.Options;

namespace Dashboard.Bff.Aggregation;

/// <summary>
/// Builds an aggregated <see cref="DashboardSnapshot"/> by concurrently fanning out to
/// <see cref="ISalesClient"/> and <see cref="IInventoryClient"/>. This is the only component
/// that references those downstream clients.
/// </summary>
public sealed class DashboardSnapshotBuilder(
    ISalesClient sales,
    IInventoryClient inventory,
    IClock clock,
    IOptions<DashboardInventoryOptions> inventoryOptions) : IDashboardSnapshotBuilder
{
    public async Task<DashboardSnapshot> BuildAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);
        var tomorrowStart = todayStart.AddDays(1);
        var weekStart = todayStart.AddDays(-6);

        var recentOrdersTask = sales.GetRecentOrdersAsync(5, cancellationToken);
        var todayOrdersTask = sales.GetOrdersInRangeAsync(todayStart, tomorrowStart, cancellationToken);
        var chartOrdersTask = sales.GetOrdersInRangeAsync(weekStart, tomorrowStart, cancellationToken);
        var orderCountTask = sales.GetOrderCountAsync(cancellationToken);
        var pendingOrderCountTask = sales.GetPendingOrderCountAsync(cancellationToken);
        var productCountTask = sales.GetProductCountAsync(cancellationToken);
        var publishedProductCountTask = sales.GetPublishedProductCountAsync(cancellationToken);
        var customerCountTask = sales.GetCustomerCountAsync(cancellationToken);
        var inventoryTask = inventory.GetInventorySummaryAsync(inventoryOptions.Value.LowStockThreshold, cancellationToken);

        await Task.WhenAll(
            recentOrdersTask,
            todayOrdersTask,
            chartOrdersTask,
            orderCountTask,
            pendingOrderCountTask,
            productCountTask,
            publishedProductCountTask,
            customerCountTask,
            inventoryTask);

        var metrics = new DashboardMetrics(
            orderCountTask.Result,
            pendingOrderCountTask.Result,
            todayOrdersTask.Result.Sum(o => o.Total),
            customerCountTask.Result,
            productCountTask.Result,
            publishedProductCountTask.Result);

        return new DashboardSnapshot(
            metrics,
            inventoryTask.Result,
            recentOrdersTask.Result,
            chartOrdersTask.Result
                .Select(o => new OrderChartPointDto(o.CreatedAt, o.Total, o.Status))
                .ToList(),
            now);
    }
}
