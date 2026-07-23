using Dashboard.Bff.Contracts;
using Dashboard.Bff.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Dashboard.Bff.Caching;

/// <summary>
/// In-memory dashboard snapshot cache used when Redis is unavailable or disabled.
/// </summary>
public sealed class MemoryDashboardSnapshotCache : IDashboardSnapshotCache
{
    private readonly IMemoryCache _cache;
    private readonly IOptions<DashboardCacheOptions> _options;

    /// <summary>
    /// Initializes the in-memory dashboard snapshot cache.
    /// </summary>
    /// <param name="cache">Memory cache.</param>
    /// <param name="options">Dashboard cache options.</param>
    public MemoryDashboardSnapshotCache(IMemoryCache cache, IOptions<DashboardCacheOptions> options)
    {
        _cache = cache;
        _options = options;
    }

    /// <inheritdoc/>
    public Task<DashboardSnapshot?> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_cache.Get<DashboardSnapshot>(_options.Value.Key));
    }

    /// <inheritdoc/>
    public Task SetAsync(DashboardSnapshot snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _cache.Set(
            _options.Value.Key,
            snapshot,
            TimeSpan.FromSeconds(_options.Value.TtlSeconds));
        return Task.CompletedTask;
    }
}
