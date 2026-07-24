using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Infrastructure.Coordination.Redis;

/// <summary>
/// Registers the shared Redis-backed <see cref="IDistributedLeaseManager"/>.
/// </summary>
public static class RedisDistributedLeaseServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IDistributedLeaseManager"/> backed by Redis. Reuses the
    /// <see cref="StackExchange.Redis.IConnectionMultiplexer"/> already registered by the caller;
    /// this does not open a new Redis connection.
    /// </summary>
    public static IServiceCollection AddRedisDistributedLeases(this IServiceCollection services)
    {
        services.TryAddSingleton<IDistributedLeaseManager, RedisDistributedLeaseManager>();
        return services;
    }
}
