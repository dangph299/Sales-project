using BuildingBlocks.Web;
using BuildingBlocks.Web.OpenApi;
using Hangfire;
using Sales.Api.Filters;
using Serilog;

namespace Sales.Api.Extensions;

/// <summary>
/// Configures the Sales API HTTP middleware pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Applies exception handling, request logging, observability, authentication, authorization,
    /// Hangfire dashboard, Swagger, and controller mapping.
    /// </summary>
    /// <param name="app">
    /// The Sales API application.
    /// </param>
    /// <returns>
    /// The same application, to allow chaining.
    /// </returns>
    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging(RequestLoggingDefaults.Configure);
        app.UseMiddleware<RequestObservabilityMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new LocalDashboardAuthorizationFilter()]
        });
        app.UseApiDocumentation("Sales API");
        app.MapControllers();

        return app;
    }
}
