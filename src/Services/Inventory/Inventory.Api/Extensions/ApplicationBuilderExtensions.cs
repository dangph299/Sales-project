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
    /// Applies request logging, observability, authentication, authorization, Swagger, and controller mapping.
    /// </summary>
    /// <param name="app">
    /// The Inventory API application.
    /// </param>
    /// <returns>
    /// The same application, to allow chaining.
    /// </returns>
    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        app.UseSerilogRequestLogging(RequestLoggingDefaults.Configure);
        app.UseMiddleware<RequestObservabilityMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseApiDocumentation("Inventory API");
        app.MapControllers();

        return app;
    }
}
