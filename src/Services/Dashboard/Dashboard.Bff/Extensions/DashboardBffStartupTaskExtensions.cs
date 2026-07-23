namespace Dashboard.Bff.Extensions;

/// <summary>
/// Startup tasks run once after the Dashboard BFF host is built and before it starts serving.
/// </summary>
public static class DashboardBffStartupTaskExtensions
{
    /// <summary>
    /// Runs the Dashboard BFF startup tasks. Currently a no-op placeholder; recurring-job
    /// (Hangfire) registration is added in a later phase.
    /// </summary>
    /// <param name="app">Dashboard BFF application.</param>
    /// <returns>A completed task.</returns>
    public static Task RunDashboardStartupTasksAsync(this WebApplication app)
    {
        _ = app;
        return Task.CompletedTask;
    }
}
