using BuildingBlocks.Application;
using Dashboard.Bff.Aggregation;
using Dashboard.Bff.Clients;
using Dashboard.Bff.Contracts;
using Dashboard.Bff.Options;
using Microsoft.Extensions.Options;

namespace Dashboard.Bff.Tests;

/// <summary>
/// Exercises <see cref="DashboardSnapshotBuilder"/>: field mapping and concurrent fan-out to downstream clients.
/// </summary>
public sealed class DashboardSnapshotBuilderTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-23T15:30:00Z");

    [Fact]
    public async Task BuildAsync_maps_every_field_from_downstream_clients()
    {
        var recentOrders = new List<RecentOrderDto>
        {
            new(Guid.NewGuid(), "ORD-1", "Alice", "Completed", 3, 120.50m, Now.AddHours(-1)),
        };

        var todayOrders = new List<OrderChartPointDto>
        {
            new(Now.AddHours(-2), 100m, "Completed"),
            new(Now.AddHours(-1), 50.25m, "Pending"),
        };

        var weekOrders = new List<OrderChartPointDto>
        {
            new(Now.AddDays(-6), 10m, "Completed"),
            new(Now.AddHours(-2), 100m, "Completed"),
            new(Now.AddHours(-1), 50.25m, "Pending"),
        };

        var inventory = new InventorySummaryDto(10, 500, 7, 2, 1, 5);

        var sales = new FakeSalesClient
        {
            RecentOrders = recentOrders,
            TodayOrders = todayOrders,
            WeekOrders = weekOrders,
            OrderCount = 42,
            PendingOrderCount = 3,
            ProductCount = 20,
            PublishedProductCount = 15,
            CustomerCount = 8,
        };

        var inventoryClient = new FakeInventoryClient { Summary = inventory };
        var clock = new FakeClock(Now);
        var options = Microsoft.Extensions.Options.Options.Create(new DashboardInventoryOptions { LowStockThreshold = 5 });

        var builder = new DashboardSnapshotBuilder(sales, inventoryClient, clock, options);

        var snapshot = await builder.BuildAsync(CancellationToken.None);

        Assert.Equal(42, snapshot.Metrics.OrderTotal);
        Assert.Equal(3, snapshot.Metrics.PendingOrderCount);
        Assert.Equal(150.25m, snapshot.Metrics.RevenueToday);
        Assert.Equal(8, snapshot.Metrics.CustomerTotal);
        Assert.Equal(20, snapshot.Metrics.ProductTotal);
        Assert.Equal(15, snapshot.Metrics.PublishedProductCount);

        Assert.Same(inventory, snapshot.Inventory);

        Assert.Equal(recentOrders, snapshot.RecentOrders);

        Assert.Equal(weekOrders.Count, snapshot.OrderChart.Count);
        for (var i = 0; i < weekOrders.Count; i++)
        {
            Assert.Equal(weekOrders[i].CreatedAt, snapshot.OrderChart[i].CreatedAt);
            Assert.Equal(weekOrders[i].Total, snapshot.OrderChart[i].Total);
            Assert.Equal(weekOrders[i].Status, snapshot.OrderChart[i].Status);
        }

        Assert.Equal(Now, snapshot.RefreshedAt);

        // GetRecentOrdersAsync should be called with take=5 per the brief.
        Assert.Equal(5, sales.RecentOrdersTakeArg);

        // Inventory client should receive the configured low-stock threshold.
        Assert.Equal(5, inventoryClient.LowStockThresholdArg);

        // Today range: [todayStart, tomorrowStart); week range: [todayStart-6d, tomorrowStart).
        var expectedTodayStart = new DateTimeOffset(Now.Date, TimeSpan.Zero);
        var expectedTomorrowStart = expectedTodayStart.AddDays(1);
        var expectedWeekStart = expectedTodayStart.AddDays(-6);

        Assert.Equal(expectedTodayStart, sales.TodayRangeFromArg);
        Assert.Equal(expectedTomorrowStart, sales.TodayRangeToArg);
        Assert.Equal(expectedWeekStart, sales.WeekRangeFromArg);
        Assert.Equal(expectedTomorrowStart, sales.WeekRangeToArg);
    }

    [Fact]
    public async Task BuildAsync_calls_all_nine_downstream_methods_concurrently()
    {
        var gate = new ConcurrencyGate(expectedConcurrentCallers: 9);

        var sales = new GatedSalesClient(gate);
        var inventory = new GatedInventoryClient(gate);
        var clock = new FakeClock(Now);
        var options = Microsoft.Extensions.Options.Options.Create(new DashboardInventoryOptions { LowStockThreshold = 5 });

        var builder = new DashboardSnapshotBuilder(sales, inventory, clock, options);

        var snapshot = await builder.BuildAsync(CancellationToken.None);

        Assert.NotNull(snapshot);
        Assert.True(gate.AllArrivedSimultaneously, "Expected all 9 downstream calls to be in-flight simultaneously, proving concurrent fan-out rather than sequential awaits.");
    }

    /// <summary>
    /// Shared gate that every fake client call must pass through. Each call increments an arrival
    /// counter and then waits until all expected callers have arrived, proving they were all
    /// started (and in-flight) before any of them is allowed to complete.
    /// </summary>
    private sealed class ConcurrencyGate(int expectedConcurrentCallers)
    {
        private readonly TaskCompletionSource _allArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivedCount;

        public bool AllArrivedSimultaneously { get; private set; }

        public async Task ArriveAndWaitAsync()
        {
            var arrived = Interlocked.Increment(ref _arrivedCount);
            if (arrived == expectedConcurrentCallers)
            {
                AllArrivedSimultaneously = true;
                _allArrived.SetResult();
            }

            // Every caller waits for all others to arrive before any is allowed to proceed.
            // If the production code awaits sequentially instead of fanning out, this will
            // deadlock/timeout because fewer than 9 callers will ever be in-flight at once.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await using var registration = cts.Token.Register(() =>
                throw new TimeoutException(
                    $"Only {Volatile.Read(ref _arrivedCount)} of {expectedConcurrentCallers} downstream calls were in-flight simultaneously."));

            await _allArrived.Task;
        }
    }

    private sealed class GatedSalesClient(ConcurrencyGate gate) : ISalesClient
    {
        public async Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersAsync(int take, CancellationToken cancellationToken)
        {
            await gate.ArriveAndWaitAsync();
            return [];
        }

        public async Task<IReadOnlyList<OrderChartPointDto>> GetOrdersInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
        {
            await gate.ArriveAndWaitAsync();
            return [];
        }

        public async Task<long> GetOrderCountAsync(CancellationToken cancellationToken)
        {
            await gate.ArriveAndWaitAsync();
            return 0;
        }

        public async Task<long> GetPendingOrderCountAsync(CancellationToken cancellationToken)
        {
            await gate.ArriveAndWaitAsync();
            return 0;
        }

        public async Task<long> GetProductCountAsync(CancellationToken cancellationToken)
        {
            await gate.ArriveAndWaitAsync();
            return 0;
        }

        public async Task<long> GetPublishedProductCountAsync(CancellationToken cancellationToken)
        {
            await gate.ArriveAndWaitAsync();
            return 0;
        }

        public async Task<long> GetCustomerCountAsync(CancellationToken cancellationToken)
        {
            await gate.ArriveAndWaitAsync();
            return 0;
        }
    }

    private sealed class GatedInventoryClient(ConcurrencyGate gate) : IInventoryClient
    {
        public async Task<InventorySummaryDto> GetInventorySummaryAsync(int lowStockThreshold, CancellationToken cancellationToken)
        {
            await gate.ArriveAndWaitAsync();
            return new InventorySummaryDto(0, 0, 0, 0, 0, lowStockThreshold);
        }
    }

    // GetOrdersInRangeAsync is called twice (today + week range), so the concurrency test above
    // must observe: recent(1) + range(2) + orderCount(1) + pending(1) + productCount(1) +
    // published(1) + customerCount(1) + inventory(1) = 9 in-flight calls.

    /// <summary>Hand-rolled fake sales client recording arguments and returning canned data.</summary>
    private sealed class FakeSalesClient : ISalesClient
    {
        public IReadOnlyList<RecentOrderDto> RecentOrders { get; set; } = [];
        public IReadOnlyList<OrderChartPointDto> TodayOrders { get; set; } = [];
        public IReadOnlyList<OrderChartPointDto> WeekOrders { get; set; } = [];
        public long OrderCount { get; set; }
        public long PendingOrderCount { get; set; }
        public long ProductCount { get; set; }
        public long PublishedProductCount { get; set; }
        public long CustomerCount { get; set; }

        public int RecentOrdersTakeArg { get; private set; }
        public DateTimeOffset TodayRangeFromArg { get; private set; }
        public DateTimeOffset TodayRangeToArg { get; private set; }
        public DateTimeOffset WeekRangeFromArg { get; private set; }
        public DateTimeOffset WeekRangeToArg { get; private set; }

        public Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersAsync(int take, CancellationToken cancellationToken)
        {
            RecentOrdersTakeArg = take;
            return Task.FromResult(RecentOrders);
        }

        public Task<IReadOnlyList<OrderChartPointDto>> GetOrdersInRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
        {
            // Distinguish the "today" call (1-day span) from the "week" call (7-day span) by range width.
            if (to - from <= TimeSpan.FromDays(1))
            {
                TodayRangeFromArg = from;
                TodayRangeToArg = to;
                return Task.FromResult(TodayOrders);
            }

            WeekRangeFromArg = from;
            WeekRangeToArg = to;
            return Task.FromResult(WeekOrders);
        }

        public Task<long> GetOrderCountAsync(CancellationToken cancellationToken) => Task.FromResult(OrderCount);

        public Task<long> GetPendingOrderCountAsync(CancellationToken cancellationToken) => Task.FromResult(PendingOrderCount);

        public Task<long> GetProductCountAsync(CancellationToken cancellationToken) => Task.FromResult(ProductCount);

        public Task<long> GetPublishedProductCountAsync(CancellationToken cancellationToken) => Task.FromResult(PublishedProductCount);

        public Task<long> GetCustomerCountAsync(CancellationToken cancellationToken) => Task.FromResult(CustomerCount);
    }

    /// <summary>Hand-rolled fake inventory client recording arguments and returning canned data.</summary>
    private sealed class FakeInventoryClient : IInventoryClient
    {
        public InventorySummaryDto Summary { get; set; } = new(0, 0, 0, 0, 0, 0);

        public int LowStockThresholdArg { get; private set; }

        public Task<InventorySummaryDto> GetInventorySummaryAsync(int lowStockThreshold, CancellationToken cancellationToken)
        {
            LowStockThresholdArg = lowStockThreshold;
            return Task.FromResult(Summary);
        }
    }

    /// <summary>Hand-rolled fake clock with a settable <see cref="UtcNow"/>.</summary>
    private sealed class FakeClock(DateTimeOffset initial) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = initial;
    }
}
