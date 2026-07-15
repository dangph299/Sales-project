namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Shared Hangfire queue names used by service hosts.
/// </summary>
public static class HangfireQueueNames
{
    /// <summary>Queue for business-critical background work.</summary>
    public const string Critical = "critical";

    /// <summary>Default Hangfire queue.</summary>
    public const string Default = "default";

    /// <summary>Queue for maintenance and scheduled housekeeping work.</summary>
    public const string Maintenance = "maintenance";
}
