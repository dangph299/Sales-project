namespace Dashboard.Bff.Options;

/// <summary>
/// Settings for the recurring background job that refreshes the cached dashboard snapshot.
/// </summary>
public sealed class DashboardRefreshJobOptions
{
    public const string SectionName = "Dashboard:RefreshJob";

    /// <summary>When <see langword="true"/>, the recurring refresh job is scheduled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Cron expression controlling how often the refresh job runs.</summary>
    public string Cron { get; set; } = "* * * * *";

    /// <summary>Hangfire queue the refresh job is enqueued on.</summary>
    public string Queue { get; set; } = "default";
}
