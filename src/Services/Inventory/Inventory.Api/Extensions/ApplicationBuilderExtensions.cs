using BuildingBlocks.Web;
using BuildingBlocks.Web.OpenApi;
using Serilog;

namespace Inventory.Api.Extensions;

/// <summary>
/// Configures the Inventory API HTTP middleware pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Applies exception handling, request logging, observability, authentication, authorization,
    /// Swagger, and controller mapping.
    /// </summary>
    /// <param name="app">Inventory API application.</param>
    /// <returns>Application for chaining.</returns>
    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging(RequestLoggingDefaults.Configure);
        app.UseMiddleware<RequestObservabilityMiddleware>();
        app.UseRouting();
        app.UseSwaggerCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseApiDocumentation("Inventory API");
        app.MapControllers();

        return app;
    }
}
