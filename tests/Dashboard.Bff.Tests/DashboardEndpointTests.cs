using BuildingBlocks.Web.Models;
using Dashboard.Bff.Aggregation;
using Dashboard.Bff.Caching;
using Dashboard.Bff.Contracts;
using Dashboard.Bff.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Bff.Tests;

public sealed class DashboardEndpointTests
{
    [Fact]
    public async Task Get_returns_cached_snapshot_without_calling_builder_on_cache_hit()
    {
        var snapshot = Snapshot();
        var builder = new FakeDashboardSnapshotBuilder(Snapshot(orderTotal: 99));
        var cache = new FakeDashboardSnapshotCache { Cached = snapshot };
        var controller = new DashboardController(builder, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Get(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<DashboardSnapshot>>(okResult.Value);

        Assert.Same(snapshot, response.Data);
        Assert.Equal(0, builder.BuildCalls);
        Assert.Equal(1, cache.GetCalls);
        Assert.Equal(0, cache.SetCalls);
    }

    [Fact]
    public async Task Get_builds_stores_and_returns_snapshot_on_cache_miss()
    {
        var snapshot = Snapshot();
        var builder = new FakeDashboardSnapshotBuilder(snapshot);
        var cache = new FakeDashboardSnapshotCache();
        var controller = new DashboardController(builder, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Get(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<DashboardSnapshot>>(okResult.Value);

        Assert.Same(snapshot, response.Data);
        Assert.Equal(1, builder.BuildCalls);
        Assert.Equal(1, cache.GetCalls);
        Assert.Equal(1, cache.SetCalls);
        Assert.Same(snapshot, cache.Stored);
    }

    [Fact]
    public void DashboardController_requires_authorization()
    {
        var attribute = Assert.Single(
            typeof(DashboardController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .OfType<AuthorizeAttribute>());

        Assert.NotNull(attribute);
    }

    private static DashboardSnapshot Snapshot(long orderTotal = 42)
    {
        var now = DateTimeOffset.Parse("2026-07-23T15:30:00Z");
        return new DashboardSnapshot(
            new DashboardMetrics(orderTotal, 3, 150.25m, 8, 20, 15),
            new InventorySummaryDto(10, 500, 7, 2, 1, 5),
            [new RecentOrderDto(Guid.NewGuid(), "ORD-1", "Alice", "Completed", 3, 120.50m, now)],
            [new OrderChartPointDto(now, 120.50m, "Completed")],
            now);
    }

    private sealed class FakeDashboardSnapshotBuilder(DashboardSnapshot snapshot) : IDashboardSnapshotBuilder
    {
        public int BuildCalls { get; private set; }

        public Task<DashboardSnapshot> BuildAsync(CancellationToken cancellationToken)
        {
            BuildCalls++;
            return Task.FromResult(snapshot);
        }
    }

    private sealed class FakeDashboardSnapshotCache : IDashboardSnapshotCache
    {
        public DashboardSnapshot? Cached { get; set; }
        public DashboardSnapshot? Stored { get; private set; }
        public int GetCalls { get; private set; }
        public int SetCalls { get; private set; }

        public Task<DashboardSnapshot?> GetAsync(CancellationToken cancellationToken)
        {
            GetCalls++;
            return Task.FromResult(Cached);
        }

        public Task SetAsync(DashboardSnapshot snapshot, CancellationToken cancellationToken)
        {
            SetCalls++;
            Stored = snapshot;
            return Task.CompletedTask;
        }
    }
}
