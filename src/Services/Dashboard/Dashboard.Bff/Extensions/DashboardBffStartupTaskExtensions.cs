using BuildingBlocks.Infrastructure;
using Dashboard.Bff.Jobs;
using Dashboard.Bff.Options;
using Hangfire;
using Microsoft.Extensions.Options;

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
        app.Services.RegisterDashboardRecurringJobs();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Declares every Dashboard BFF recurring job.
    /// </summary>
    /// <param name="serviceProvider">Application service provider.</param>
    public static void RegisterDashboardRecurringJobs(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var serviceScope = serviceProvider.CreateScope();
        var recurringJobManager = serviceScope.ServiceProvider.GetService<IRecurringJobManager>();
        if (recurringJobManager is null)
        {
            return;
        }

        var options = serviceScope.ServiceProvider.GetRequiredService<IOptions<DashboardRefreshJobOptions>>().Value;
        recurringJobManager.ScheduleRecurringJob<DashboardSnapshotRefreshJob>(
            DashboardRecurringJobIds.SnapshotRefresh,
            options.ToRecurringJobSettings(),
            job => job.ExecuteAsync(CancellationToken.None));
    }
}
