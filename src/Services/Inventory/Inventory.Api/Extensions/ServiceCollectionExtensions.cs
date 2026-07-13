using BuildingBlocks.Observability;
using BuildingBlocks.Web.Authentication;
using BuildingBlocks.Web.Observability;
using BuildingBlocks.Web.OpenApi;
using Inventory.Infrastructure;
using Serilog;

namespace Inventory.Api.Extensions;

/// <summary>
/// Composition extensions for the Inventory API host.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all services required by the Inventory API host.
    /// </summary>
    /// <param name="builder">Inventory API web application builder.</param>
    /// <returns>Builder for chaining.</returns>
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, config) =>
            config.ConfigureSharedSinks(context.Configuration, "inventory-api"));

        builder.Services.AddProblemDetails();
        builder.Services.AddControllers();
        builder.Services.AddApiDocumentation(
            "Inventory API",
            "Inventory service API for stock queries, reservations, and stock adjustments.");
        builder.Services.AddSwaggerCors(builder.Environment);
        builder.Services.AddInventoryInfrastructure(builder.Configuration);
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddApplicationObservability(
            builder.Configuration,
            InventoryObservability.KafkaActivitySourceName,
            "Inventory.Infrastructure");

        return builder;
    }
}
