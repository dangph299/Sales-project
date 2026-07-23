using System.Text.Json;
using Dashboard.Bff.Contracts;
using Dashboard.Bff.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Dashboard.Bff.Caching;

/// <summary>
/// Redis-backed dashboard snapshot cache.
/// </summary>
public sealed class RedisDashboardSnapshotCache : IDashboardSnapshotCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly IOptions<DashboardCacheOptions> _options;

    /// <summary>
    /// Initializes the Redis dashboard snapshot cache.
    /// </summary>
    /// <param name="cache">Distributed cache.</param>
    /// <param name="options">Dashboard cache options.</param>
    public RedisDashboardSnapshotCache(IDistributedCache cache, IOptions<DashboardCacheOptions> options)
    {
        _cache = cache;
        _options = options;
    }

    /// <inheritdoc/>
    public async Task<DashboardSnapshot?> GetAsync(CancellationToken cancellationToken)
    {
        var json = await _cache.GetStringAsync(_options.Value.Key, cancellationToken);
        return json is null ? null : JsonSerializer.Deserialize<DashboardSnapshot>(json, JsonOptions);
    }

    /// <inheritdoc/>
    public Task SetAsync(DashboardSnapshot snapshot, CancellationToken cancellationToken)
    {
        return _cache.SetStringAsync(
            _options.Value.Key,
            JsonSerializer.Serialize(snapshot, JsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.Value.TtlSeconds)
            },
            cancellationToken);
    }
}
