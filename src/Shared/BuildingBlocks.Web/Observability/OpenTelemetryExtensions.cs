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
    /// Registers service telemetry shared by API hosts.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration. Kept in the signature for consistent host composition.</param>
    /// <param name="activitySourceName">Service-specific activity source name to include in tracing.</param>
    /// <param name="meterName">Service-specific meter name to include in metrics.</param>
    /// <returns>Service collection for chaining.</returns>
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
