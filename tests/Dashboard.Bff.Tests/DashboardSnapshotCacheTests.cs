using Dashboard.Bff.Caching;
using Dashboard.Bff.Contracts;
using Dashboard.Bff.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Dashboard.Bff.Tests;

public sealed class DashboardSnapshotCacheTests
{
    [Fact]
    public async Task Memory_cache_returns_null_before_a_snapshot_is_stored()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryDashboardSnapshotCache(memory, CacheOptions("dashboard:snapshot"));

        var result = await cache.GetAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Memory_cache_round_trips_the_snapshot()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryDashboardSnapshotCache(memory, CacheOptions("dashboard:snapshot"));
        var snapshot = Snapshot();

        await cache.SetAsync(snapshot, CancellationToken.None);

        var result = await cache.GetAsync(CancellationToken.None);

        Assert.Same(snapshot, result);
    }

    [Fact]
    public async Task Memory_cache_uses_the_configured_key()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var first = new MemoryDashboardSnapshotCache(memory, CacheOptions("dashboard:snapshot"));
        var second = new MemoryDashboardSnapshotCache(memory, CacheOptions("dashboard:other"));
        var snapshot = Snapshot();

        await first.SetAsync(snapshot, CancellationToken.None);

        Assert.Same(snapshot, await first.GetAsync(CancellationToken.None));
        Assert.Null(await second.GetAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Redis_cache_round_trips_the_snapshot_as_json()
    {
        var memory = new MemoryDistributedCache(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
        var cache = new RedisDashboardSnapshotCache(memory, CacheOptions("dashboard:snapshot"));
        var snapshot = Snapshot();

        await cache.SetAsync(snapshot, CancellationToken.None);

        var result = await cache.GetAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(snapshot.Metrics.OrderTotal, result!.Metrics.OrderTotal);
        Assert.Equal(snapshot.Inventory.TotalQuantity, result.Inventory.TotalQuantity);
        Assert.Equal(snapshot.RecentOrders[0].OrderCode, result.RecentOrders[0].OrderCode);
        Assert.Equal(snapshot.OrderChart[0].Total, result.OrderChart[0].Total);
        Assert.Equal(snapshot.RefreshedAt, result.RefreshedAt);
    }

    private static IOptions<DashboardCacheOptions> CacheOptions(string key) =>
        Microsoft.Extensions.Options.Options.Create(new DashboardCacheOptions
        {
            Key = key,
            TtlSeconds = 300
        });

    private static DashboardSnapshot Snapshot()
    {
        var now = DateTimeOffset.Parse("2026-07-23T15:30:00Z");
        return new DashboardSnapshot(
            new DashboardMetrics(42, 3, 150.25m, 8, 20, 15),
            new InventorySummaryDto(10, 500, 7, 2, 1, 5),
            [new RecentOrderDto(Guid.Parse("11111111-1111-1111-1111-111111111111"), "ORD-1", "Alice", "Completed", 3, 120.50m, now)],
            [new OrderChartPointDto(now, 120.50m, "Completed")],
            now);
    }
}
