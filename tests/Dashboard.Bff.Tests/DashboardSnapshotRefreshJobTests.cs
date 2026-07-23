using Dashboard.Bff.Aggregation;
using Dashboard.Bff.Caching;
using Dashboard.Bff.Contracts;
using Dashboard.Bff.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dashboard.Bff.Tests;

public sealed class DashboardSnapshotRefreshJobTests
{
    [Fact]
    public async Task ExecuteAsync_builds_then_stores_the_snapshot()
    {
        var calls = new List<string>();
        var snapshot = Snapshot();
        var builder = new RecordingDashboardSnapshotBuilder(snapshot, calls);
        var cache = new RecordingDashboardSnapshotCache(calls);
        var job = new DashboardSnapshotRefreshJob(
            builder,
            cache,
            NullLogger<DashboardSnapshotRefreshJob>.Instance);

        await job.ExecuteAsync(CancellationToken.None);

        Assert.Equal(["build", "set"], calls);
        Assert.Equal(1, builder.BuildCalls);
        Assert.Equal(1, cache.SetCalls);
        Assert.Same(snapshot, cache.Stored);
    }

    [Fact]
    public void SnapshotRefresh_job_id_is_stable()
    {
        Assert.Equal("dashboard:snapshot-refresh", DashboardRecurringJobIds.SnapshotRefresh);
    }

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

    private sealed class RecordingDashboardSnapshotBuilder(
        DashboardSnapshot snapshot,
        List<string> calls) : IDashboardSnapshotBuilder
    {
        public int BuildCalls { get; private set; }

        public Task<DashboardSnapshot> BuildAsync(CancellationToken cancellationToken)
        {
            calls.Add("build");
            BuildCalls++;
            return Task.FromResult(snapshot);
        }
    }

    private sealed class RecordingDashboardSnapshotCache(List<string> calls) : IDashboardSnapshotCache
    {
        public DashboardSnapshot? Stored { get; private set; }
        public int SetCalls { get; private set; }

        public Task<DashboardSnapshot?> GetAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Refresh job only writes the snapshot cache.");
        }

        public Task SetAsync(DashboardSnapshot snapshot, CancellationToken cancellationToken)
        {
            calls.Add("set");
            Stored = snapshot;
            SetCalls++;
            return Task.CompletedTask;
        }
    }
}
