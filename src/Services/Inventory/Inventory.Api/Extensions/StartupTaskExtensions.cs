using BuildingBlocks.Infrastructure;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Extensions;

/// <summary>
/// Startup tasks for infrastructure components required by the Inventory API host.
/// </summary>
public static class StartupTaskExtensions
{
    /// <summary>
    /// Runs Inventory startup tasks before serving traffic.
    /// </summary>
    /// <param name="app">Inventory API application.</param>
    public static async Task RunStartupTasksAsync(this WebApplication app)
    {
        await using (var scope = app.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
        }

        var bus = await KafkaBusLifecycle.StartAsync(app.Services);
        app.Lifetime.ApplicationStopping.Register(() => KafkaBusLifecycle.StopAsync(bus).GetAwaiter().GetResult());
    }
}
