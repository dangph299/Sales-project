namespace Dashboard.Bff.Options;

/// <summary>
/// Caching settings for the aggregated dashboard snapshot.
/// </summary>
public sealed class DashboardCacheOptions
{
    public const string SectionName = "Dashboard:Cache";

    /// <summary>Cache key under which the dashboard snapshot is stored.</summary>
    public string Key { get; set; } = "dashboard:snapshot";

    /// <summary>Time-to-live, in seconds, for the cached dashboard snapshot.</summary>
    public int TtlSeconds { get; set; } = 300;

    /// <summary>When <see langword="true"/>, the snapshot cache is backed by Redis; otherwise an in-memory cache is used.</summary>
    public bool UseRedis { get; set; } = true;
}
