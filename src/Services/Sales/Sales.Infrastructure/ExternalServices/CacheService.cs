using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Sales.Application;

namespace Sales.Infrastructure;

/// <summary>
/// Shared cache adapter for read-model values.
/// </summary>
public abstract class CacheService<T> : ICacheService<T>
{
    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes the cache service with its backing distributed cache.
    /// </summary>
    /// <param name="cache">Distributed cache.</param>
    protected CacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Gets the absolute expiration applied to cached entries. Defaults to 10 minutes.
    /// </summary>
    protected virtual TimeSpan Ttl => TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets the cache key prefix identifying this cache's entries.
    /// </summary>
    protected abstract string KeyPrefix { get; }

    /// <summary>
    /// Extracts the unique identifier from a value, used to build its cache key.
    /// </summary>
    /// <param name="value">Value to extract the identifier from.</param>
    /// <returns>Value's unique identifier.</returns>
    protected abstract Guid GetId(T value);

    /// <inheritdoc/>
    public async Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var value = await _cache.GetStringAsync(Key(id), cancellationToken);
        return value is null ? default : JsonSerializer.Deserialize<T>(value);
    }

    /// <inheritdoc/>
    public Task SetAsync(T value, CancellationToken cancellationToken = default)
    {
        return _cache.SetStringAsync(
            Key(GetId(value)),
            JsonSerializer.Serialize(value),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl },
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _cache.RemoveAsync(Key(id), cancellationToken);
    }

    /// <summary>
    /// Builds the cache key for a given identifier.
    /// </summary>
    /// <param name="id">Unique identifier to build a key for.</param>
    /// <returns>Cache key, combining <see cref="KeyPrefix"/> and <paramref name="id"/>.</returns>
    protected virtual string Key(Guid id) => $"{KeyPrefix}:{id:N}";
}
