namespace BuildingBlocks.Infrastructure.Coordination.Redis;

/// <summary>
/// Settings for a single <see cref="IDistributedLeaseManager.TryAcquireAsync"/> attempt.
/// </summary>
public sealed record DistributedLeaseOptions
{
    /// <summary>How long the lease is held before it expires on its own if never released.</summary>
    public required TimeSpan LeaseDuration { get; init; }
}
