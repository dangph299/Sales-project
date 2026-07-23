using BuildingBlocks.Observability;
using BuildingBlocks.Web;

namespace Dashboard.Bff.Extensions;

/// <summary>
/// Composition extensions for the Dashboard BFF host.
/// </summary>
public static class DashboardBffServiceCollectionExtensions
{
    private const string ServiceName = "dashboard-bff";
    private const string ActivitySourceName = "Dashboard.Bff";
    private const string MeterName = "Dashboard.Bff";

    /// <summary>
    /// Registers all services required by the Dashboard BFF host.
    /// </summary>
    /// <param name="builder">Dashboard BFF web application builder.</param>
    /// <returns>Builder for chaining.</returns>
    public static WebApplicationBuilder AddDashboardBff(this WebApplicationBuilder builder)
    {
        builder.AddBuildingBlocksLogging(ServiceName);

        builder.Services.AddBuildingBlocksWeb(builder.Configuration, options =>
        {
            options.ServiceName = ServiceName;
            options.ApiTitle = "Dashboard BFF";
            options.ApiDescription = "Backend-for-frontend aggregating dashboard data for the Sales web client.";
            options.ActivitySourceName = ActivitySourceName;
            options.MeterName = MeterName;
        });

        builder.Services.AddHttpContextAccessor();

        return builder;
    }
}
