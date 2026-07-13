using Hangfire;
using KafkaFlow;
using Sales.Infrastructure;

namespace Sales.Api.Extensions;

/// <summary>
/// Startup tasks for infrastructure components required by the Sales API host.
/// </summary>
public static class StartupTaskExtensions
{
    /// <summary>
    /// Runs Sales startup tasks before serving traffic.
    /// </summary>
    /// <param name="app">Sales API application.</param>
    public static async Task RunStartupTasksAsync(this WebApplication app)
    {
        var kafkaBus = app.Services.CreateKafkaBus();
        await kafkaBus.StartAsync();
        app.Lifetime.ApplicationStopping.Register(() => kafkaBus.StopAsync().GetAwaiter().GetResult());

        await app.Services.SeedIdentityAsync();
        RecurringJob.AddOrUpdate<MaintenanceJobs>("sales-cleanup", "maintenance", x => x.CleanupAsync(), Cron.Daily);
    }
}
