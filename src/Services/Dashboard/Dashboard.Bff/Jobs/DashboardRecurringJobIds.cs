namespace Dashboard.Bff.Jobs;

/// <summary>
/// Stable recurring job identifiers owned by the Dashboard BFF.
/// </summary>
public static class DashboardRecurringJobIds
{
    /// <summary>Recurring job id for the dashboard snapshot refresh.</summary>
    public const string SnapshotRefresh = "dashboard:snapshot-refresh";
}
