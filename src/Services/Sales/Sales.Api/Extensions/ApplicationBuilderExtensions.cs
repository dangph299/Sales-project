using BuildingBlocks.Web;
using BuildingBlocks.Web.OpenApi;
using Hangfire;
using Sales.Api.Filters;
using Sales.Api.Realtime;

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
    /// <param name="app">Sales API application.</param>
    /// <returns>Application for chaining.</returns>
    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        app.UseBuildingBlocksRequestPipeline();
        app.UseCors(RealtimeServiceCollectionExtensions.SalesWebCorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new LocalDashboardAuthorizationFilter()]
        });
        app.UseApiDocumentation(
            "Sales API",
            additionalDocuments: SalesSwaggerDocumentsFactory.Create(app.Configuration));
        app.MapControllers();
        app.MapSalesRealtime();

        return app;
    }
}
