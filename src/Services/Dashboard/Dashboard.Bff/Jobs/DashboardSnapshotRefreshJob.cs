using Dashboard.Bff.Aggregation;
using Dashboard.Bff.Caching;

namespace Dashboard.Bff.Jobs;

/// <summary>
/// Hangfire adapter that refreshes the cached dashboard snapshot.
/// </summary>
public sealed class DashboardSnapshotRefreshJob(
    IDashboardSnapshotBuilder snapshotBuilder,
    IDashboardSnapshotCache snapshotCache,
    ILogger<DashboardSnapshotRefreshJob> logger)
{
    /// <summary>
    /// Builds a fresh dashboard snapshot and stores it in cache.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Dashboard snapshot refresh started");

        var snapshot = await snapshotBuilder.BuildAsync(cancellationToken);
        await snapshotCache.SetAsync(snapshot, cancellationToken);

        logger.LogInformation("Dashboard snapshot refresh completed {RefreshedAt}", snapshot.RefreshedAt);
    }
}
