using BuildingBlocks.Contracts;
using BuildingBlocks.Web.Authentication;
using BuildingBlocks.Web.ExceptionHandling;
using BuildingBlocks.Web.Extensions;
using BuildingBlocks.Web.Observability;
using BuildingBlocks.Web.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BuildingBlocks.Web;

/// <summary>
/// Composes the web-host capabilities every controller-based API shares: problem details,
/// API exception handling, the shared error catalog, controllers with the shared validation
/// response, OpenAPI, JWT authentication/authorization, and web observability. Service-specific
/// registrations stay in each host.
/// </summary>
public static class WebHostRegistration
{
    /// <summary>
    /// Registers the shared API host services. Each service supplies its identity and any
    /// specialisation hooks through <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration (used for JWT settings).</param>
    /// <param name="configure">Configures the shared web options for this service.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddBuildingBlocksWeb(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<BuildingBlocksWebOptions> configure)
    {
        var options = new BuildingBlocksWebOptions();
        configure(options);
        options.Validate();

        services.AddProblemDetails();
        if (options.ConfigureExceptions is null)
        {
            services.AddApiExceptionHandling();
        }
        else
        {
            services.AddApiExceptionHandling(options.ConfigureExceptions);
        }

        services.AddSingleton<IErrorCatalog, ErrorCatalogResolver>();

        var controllers = services.AddControllers();
        options.ConfigureControllers?.Invoke(controllers);
        services.AddSharedApiModelResponses();

        services.AddApiDocumentation(options.ApiTitle, options.ApiDescription);
        services.AddJwtAuthentication(configuration, options.JwtClockSkew);
        services.AddAuthorization();
        services.AddBuildingBlocksWebObservability(
            options.ServiceName,
            options.ActivitySourceName,
            options.MeterName);

        return services;
    }

    /// <summary>
    /// Applies the request pipeline prefix shared by every API host, in the required order:
    /// exception handling, Serilog request logging, then correlation/observability enrichment.
    /// Service-specific middleware (routing, CORS, authentication, dashboards, endpoints) stays
    /// explicit in each host so its order remains visible.
    /// </summary>
    /// <param name="app">Web application.</param>
    /// <returns>Web application for chaining.</returns>
    public static WebApplication UseBuildingBlocksRequestPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging(RequestLoggingDefaults.Configure);
        app.UseMiddleware<RequestObservabilityMiddleware>();
        return app;
    }
}
