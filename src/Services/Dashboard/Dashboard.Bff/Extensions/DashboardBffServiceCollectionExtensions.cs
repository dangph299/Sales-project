using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Observability;
using BuildingBlocks.Web;
using Dashboard.Bff.Aggregation;
using Dashboard.Bff.Auth;
using Dashboard.Bff.Caching;
using Dashboard.Bff.Clients;
using Dashboard.Bff.Jobs;
using Dashboard.Bff.Options;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        builder.Services.TryAddSingleton<IClock, SystemClock>();
        builder.Services.AddScoped<ICallerTokenAccessor, CallerTokenAccessor>();

        builder.Services.AddHttpClient("sales-auth", (provider, client) =>
        {
            var downstreamOptions = provider.GetRequiredService<IOptions<DownstreamOptions>>().Value;
            client.BaseAddress = new Uri(downstreamOptions.SalesBaseUrl);
        });
        builder.Services.AddSingleton<IServiceTokenProvider>(provider => new ServiceAccountTokenProvider(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("sales-auth"),
            provider.GetRequiredService<IOptions<ServiceAccountOptions>>(),
            provider.GetRequiredService<IClock>(),
            provider.GetRequiredService<ILogger<ServiceAccountTokenProvider>>()));

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
        builder.Services.AddDashboardSnapshotCache(builder.Configuration);

        builder.Services.AddOptions<DashboardRefreshJobOptions>()
            .Bind(builder.Configuration.GetSection(DashboardRefreshJobOptions.SectionName))
            .Validate(o => o.IsValid(), "Dashboard:RefreshJob must be disabled or have a valid cron expression and queue")
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Hangfire")),
                "ConnectionStrings:Hangfire must be configured when Dashboard:RefreshJob:Enabled is true")
            .ValidateOnStart();
        builder.Services.AddDashboardBackgroundJobs(builder.Configuration);

        builder.Services.AddOptions<DashboardInventoryOptions>()
            .Bind(builder.Configuration.GetSection(DashboardInventoryOptions.SectionName))
            .Validate(o => o.LowStockThreshold >= 0, "Dashboard:Inventory:LowStockThreshold must be greater than or equal to 0")
            .ValidateOnStart();

        builder.Services.AddScoped<IDashboardSnapshotBuilder, DashboardSnapshotBuilder>();

        return builder;
    }

    private static IServiceCollection AddDashboardSnapshotCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection(DashboardCacheOptions.SectionName).Get<DashboardCacheOptions>()
            ?? new DashboardCacheOptions();
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (cacheOptions.UseRedis && !string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
            services.AddScoped<IDashboardSnapshotCache, RedisDashboardSnapshotCache>();
            return services;
        }

        services.AddMemoryCache();
        services.AddScoped<IDashboardSnapshotCache, MemoryDashboardSnapshotCache>();
        return services;
    }

    private static IServiceCollection AddDashboardBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<DashboardSnapshotRefreshJob>();

        var refreshOptions = configuration.GetSection(DashboardRefreshJobOptions.SectionName).Get<DashboardRefreshJobOptions>()
            ?? new DashboardRefreshJobOptions();
        var hangfireConnectionString = configuration.GetConnectionString("Hangfire");
        if (string.IsNullOrWhiteSpace(hangfireConnectionString))
        {
            return services;
        }

        services.AddHangfire(config => config.UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(hangfireConnectionString)));
        services.AddHangfireServer(options =>
        {
            options.Queues =
            [
                string.IsNullOrWhiteSpace(refreshOptions.Queue) ? HangfireQueueNames.Default : refreshOptions.Queue
            ];
        });

        return services;
    }
}
