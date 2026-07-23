using BuildingBlocks.Web;
using BuildingBlocks.Web.OpenApi;

namespace Dashboard.Bff.Extensions;

/// <summary>
/// Configures the Dashboard BFF HTTP middleware pipeline.
/// </summary>
public static class DashboardBffApplicationBuilderExtensions
{
    /// <summary>
    /// Applies exception handling, request logging, observability, authentication, authorization,
    /// Swagger, and controller mapping.
    /// </summary>
    /// <param name="app">Dashboard BFF application.</param>
    /// <returns>Application for chaining.</returns>
    public static WebApplication ConfigureDashboardBff(this WebApplication app)
    {
        app.UseBuildingBlocksRequestPipeline();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseApiDocumentation("Dashboard BFF");
        app.MapControllers();

        return app;
    }
}
