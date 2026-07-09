using Inventory.Infrastructure;
using KafkaFlow;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Extensions;

/// <summary>
/// Startup tasks for infrastructure components required by the Inventory API host.
/// </summary>
public static class StartupTaskExtensions
{
    /// <summary>
    /// Applies Inventory database migrations and starts the Kafka bus.
    /// </summary>
    /// <param name="app">
    /// The Inventory API application.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous startup work.
    /// </returns>
    public static async Task RunStartupTasksAsync(this WebApplication app)
    {
        await using (var scope = app.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
        }

        var bus = app.Services.CreateKafkaBus();
        await bus.StartAsync();
        app.Lifetime.ApplicationStopping.Register(() => bus.StopAsync().GetAwaiter().GetResult());
    }
}
