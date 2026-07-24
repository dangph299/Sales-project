using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.Coordination.Redis;

/// <summary>
/// Acquires <see cref="IDistributedLease"/>s in Redis via a single <c>SET key token NX PX ttl</c>
/// attempt.
/// </summary>
public sealed class RedisDistributedLeaseManager(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<RedisDistributedLeaseManager> logger) : IDistributedLeaseManager
{
    private const string KeyPrefix = "lock:";

    public async Task<IDistributedLease?> TryAcquireAsync(
        string resource,
        DistributedLeaseOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);

        var key = KeyPrefix + resource;
        var database = connectionMultiplexer.GetDatabase();
        var ownerToken = Guid.NewGuid().ToString("N");

        var acquired = await database.StringSetAsync(key, ownerToken, options.LeaseDuration, When.NotExists);
        if (!acquired)
        {
            logger.LogDebug("Distributed lease for resource {Resource} is held by another owner", resource);
            return null;
        }

        logger.LogDebug("Acquired distributed lease for resource {Resource}", resource);
        return new RedisDistributedLease(database, key, resource, ownerToken, logger);
    }
}
