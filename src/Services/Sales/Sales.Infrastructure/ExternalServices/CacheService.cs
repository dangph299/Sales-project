using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Sales.Application;

namespace Sales.Infrastructure;

/// <summary>
/// Generic cache-aside implementation of <see cref="ICacheService{T}"/> backed by
/// <see cref="IDistributedCache"/> (Redis), serializing values as JSON strings. Concrete caches
/// only need to supply a key prefix and how to extract an identifier from a value.
/// </summary>
/// <typeparam name="T">
/// The type of value cached.
/// </typeparam>
public abstract class CacheService<T> : ICacheService<T>
{
    private readonly IDistributedCache _cache;

    /// <summary>
    /// Initializes the cache service with its backing distributed cache.
    /// </summary>
    /// <param name="cache">
    /// The distributed cache to read/write through.
    /// </param>
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
    /// <param name="value">
    /// The value to extract the identifier from.
    /// </param>
    /// <returns>
    /// The value's unique identifier.
    /// </returns>
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
    /// <param name="id">
    /// The unique identifier to build a key for.
    /// </param>
    /// <returns>
    /// The cache key, combining <see cref="KeyPrefix"/> and <paramref name="id"/>.
    /// </returns>
    protected virtual string Key(Guid id) => $"{KeyPrefix}:{id:N}";
}
