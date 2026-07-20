using BuildingBlocks.Infrastructure;
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
        var kafkaBus = await KafkaBusLifecycle.StartAsync(app.Services);
        app.Lifetime.ApplicationStopping.Register(() => KafkaBusLifecycle.StopAsync(kafkaBus).GetAwaiter().GetResult());

        await app.Services.SeedIdentityAsync();
        app.Services.RegisterSalesRecurringJobs();
    }
}
