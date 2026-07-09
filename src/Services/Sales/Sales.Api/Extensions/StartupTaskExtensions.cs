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
    /// Starts Kafka, seeds Identity data, and schedules recurring maintenance jobs.
    /// </summary>
    /// <param name="app">
    /// The Sales API application.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous startup work.
    /// </returns>
    public static async Task RunStartupTasksAsync(this WebApplication app)
    {
        var kafkaBus = app.Services.CreateKafkaBus();
        await kafkaBus.StartAsync();
        app.Lifetime.ApplicationStopping.Register(() => kafkaBus.StopAsync().GetAwaiter().GetResult());

        await app.Services.SeedIdentityAsync();
        RecurringJob.AddOrUpdate<MaintenanceJobs>("sales-cleanup", "maintenance", x => x.CleanupAsync(), Cron.Daily);
    }
}
