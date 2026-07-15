using BuildingBlocks.Contracts;
using BuildingBlocks.Observability;
using BuildingBlocks.Web;
using Inventory.Api.Middleware;
using Inventory.Application;
using Inventory.Infrastructure;

namespace Inventory.Api.Extensions;

/// <summary>
/// Composition extensions for the Inventory API host.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string ServiceName = "inventory-api";

    /// <summary>
    /// Registers all services required by the Inventory API host.
    /// </summary>
    /// <param name="builder">Inventory API web application builder.</param>
    /// <returns>Builder for chaining.</returns>
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.AddBuildingBlocksLogging(ServiceName);

        builder.Services.AddBuildingBlocksWeb(builder.Configuration, options =>
        {
            options.ServiceName = ServiceName;
            options.ApiTitle = "Inventory API";
            options.ApiDescription = "Inventory service API for stock queries, reservations, and stock adjustments.";
            options.ActivitySourceName = InventoryObservability.KafkaActivitySourceName;
            options.MeterName = "Inventory.Infrastructure";
        });

        builder.Services.AddSingleton<IErrorMessageProvider, InventoryErrorMessageProvider>();
        builder.Services.AddSwaggerCors(builder.Environment);
        builder.Services.AddInventoryApplication();
        builder.Services.AddInventoryInfrastructure(builder.Configuration);

        return builder;
    }
}
