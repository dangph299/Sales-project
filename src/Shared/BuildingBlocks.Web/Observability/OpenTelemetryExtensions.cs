using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Web.Observability;

/// <summary>
/// Shared OpenTelemetry registration for HTTP API hosts.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Registers ASP.NET Core, HTTP client, EF Core, runtime, and service-specific telemetry.
    /// </summary>
    /// <param name="services">
    /// The service collection to register into.
    /// </param>
    /// <param name="configuration">
    /// The application configuration. Kept in the signature for consistent host composition.
    /// </param>
    /// <param name="activitySourceName">
    /// The service-specific activity source name to include in tracing.
    /// </param>
    /// <param name="meterName">
    /// The service-specific meter name to include in metrics.
    /// </param>
    /// <returns>
    /// The same service collection, to allow chaining.
    /// </returns>
    public static IServiceCollection AddApplicationObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string activitySourceName,
        string meterName)
    {
        _ = configuration;

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource(activitySourceName)
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(meterName)
                .AddOtlpExporter());

        return services;
    }
}
