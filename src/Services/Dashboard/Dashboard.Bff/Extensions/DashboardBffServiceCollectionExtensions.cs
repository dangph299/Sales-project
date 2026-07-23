using BuildingBlocks.Observability;
using BuildingBlocks.Web;
using Dashboard.Bff.Auth;
using Dashboard.Bff.Clients;
using Dashboard.Bff.Options;
using Microsoft.Extensions.Options;

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

        builder.Services.AddOptions<DownstreamOptions>()
            .Bind(builder.Configuration.GetSection(DownstreamOptions.SectionName))
            .Validate(
                o => Uri.TryCreate(o.SalesBaseUrl, UriKind.Absolute, out _),
                "Downstream:SalesBaseUrl must be a non-empty absolute URI")
            .Validate(
                o => Uri.TryCreate(o.InventoryBaseUrl, UriKind.Absolute, out _),
                "Downstream:InventoryBaseUrl must be a non-empty absolute URI")
            .ValidateOnStart();

        var isDevelopment = builder.Environment.IsDevelopment();
        var serviceAccountOptionsBuilder = builder.Services.AddOptions<ServiceAccountOptions>()
            .Bind(builder.Configuration.GetSection(ServiceAccountOptions.SectionName));

        if (isDevelopment)
        {
            serviceAccountOptionsBuilder.Validate(
                o => o.AllowAdminDevFallback || (!string.IsNullOrWhiteSpace(o.UserName) && !string.IsNullOrWhiteSpace(o.Password)),
                "ServiceAccount:UserName/Password must be set unless ServiceAccount:AllowAdminDevFallback is true");
        }
        else
        {
            serviceAccountOptionsBuilder.Validate(
                o => !string.IsNullOrWhiteSpace(o.UserName) && !string.IsNullOrWhiteSpace(o.Password),
                "ServiceAccount:UserName and ServiceAccount:Password are required outside Development");
        }

        serviceAccountOptionsBuilder.ValidateOnStart();

        builder.Services.AddTransient<DownstreamAuthDelegatingHandler>();

        builder.Services.AddHttpClient<ISalesClient, SalesClient>((provider, client) =>
            {
                var downstreamOptions = provider.GetRequiredService<IOptions<DownstreamOptions>>().Value;
                client.BaseAddress = new Uri(downstreamOptions.SalesBaseUrl);
            })
            .AddHttpMessageHandler<DownstreamAuthDelegatingHandler>()
            .AddStandardResilienceHandler();

        builder.Services.AddHttpClient<IInventoryClient, InventoryClient>((provider, client) =>
            {
                var downstreamOptions = provider.GetRequiredService<IOptions<DownstreamOptions>>().Value;
                client.BaseAddress = new Uri(downstreamOptions.InventoryBaseUrl);
            })
            .AddHttpMessageHandler<DownstreamAuthDelegatingHandler>()
            .AddStandardResilienceHandler();

        builder.Services.AddOptions<DashboardCacheOptions>()
            .Bind(builder.Configuration.GetSection(DashboardCacheOptions.SectionName))
            .Validate(o => o.TtlSeconds > 0, "Dashboard:Cache:TtlSeconds must be greater than 0")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Key), "Dashboard:Cache:Key must be non-empty")
            .ValidateOnStart();

        // TODO(Phase 6): validate cron syntax via shared Hangfire helper
        builder.Services.AddOptions<DashboardRefreshJobOptions>()
            .Bind(builder.Configuration.GetSection(DashboardRefreshJobOptions.SectionName))
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(o.Cron),
                "Dashboard:RefreshJob:Cron must be non-empty when Dashboard:RefreshJob:Enabled is true")
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(o.Queue),
                "Dashboard:RefreshJob:Queue must be non-empty when Dashboard:RefreshJob:Enabled is true")
            .ValidateOnStart();

        return builder;
    }
}
