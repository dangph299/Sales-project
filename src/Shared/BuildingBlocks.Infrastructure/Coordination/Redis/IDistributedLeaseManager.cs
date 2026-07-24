namespace BuildingBlocks.Infrastructure.Coordination.Redis;

/// <summary>
/// Acquires short-lived, TTL-bound leases over named resources so at most one caller across all
/// instances holds a given resource at a time.
/// </summary>
public interface IDistributedLeaseManager
{
    /// <summary>
    /// Makes a single attempt to acquire a lease over <paramref name="resource"/>.
    /// </summary>
    /// <returns>
    /// The held lease, or <see langword="null"/> if the resource is already held by another owner.
    /// </returns>
    Task<IDistributedLease?> TryAcquireAsync(
        string resource,
        DistributedLeaseOptions options,
        CancellationToken cancellationToken = default);
}
