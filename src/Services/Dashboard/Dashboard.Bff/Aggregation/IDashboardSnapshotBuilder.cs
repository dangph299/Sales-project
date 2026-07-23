using Dashboard.Bff.Contracts;

namespace Dashboard.Bff.Aggregation;

/// <summary>
/// Builds an aggregated <see cref="DashboardSnapshot"/> by fanning out to downstream clients concurrently.
/// </summary>
public interface IDashboardSnapshotBuilder
{
    /// <summary>Builds a fresh dashboard snapshot.</summary>
    Task<DashboardSnapshot> BuildAsync(CancellationToken cancellationToken);
}
