using Dashboard.Bff.Contracts;

namespace Dashboard.Bff.Caching;

/// <summary>
/// Cache port for the aggregated dashboard snapshot.
/// </summary>
public interface IDashboardSnapshotCache
{
    /// <summary>Gets the cached dashboard snapshot, when one exists.</summary>
    Task<DashboardSnapshot?> GetAsync(CancellationToken cancellationToken);

    /// <summary>Stores the dashboard snapshot.</summary>
    Task SetAsync(DashboardSnapshot snapshot, CancellationToken cancellationToken);
}
