using BuildingBlocks.Observability;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Web.Observability;

/// <summary>
/// Shared OpenTelemetry registration for HTTP API hosts. Adds the ASP.NET Core, HTTP client, and
/// Entity Framework Core instrumentation on top of the base pipeline from
/// <c>BuildingBlocks.Observability</c>.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Registers telemetry shared by API hosts: base OTLP export plus web instrumentation.
    /// </summary>
    /// <param name="serviceName">Service name reported as the OpenTelemetry resource.</param>
    /// <param name="activitySourceName">Service-specific activity source name to include in tracing.</param>
    /// <param name="meterName">Service-specific meter name to include in metrics.</param>
    public static IServiceCollection AddBuildingBlocksWebObservability(
        this IServiceCollection services,
        string serviceName,
        string activitySourceName,
        string meterName)
    {
        return services.AddBuildingBlocksObservability(
            serviceName,
            tracing => ConfigureWebTracing(tracing, activitySourceName),
            metrics => ConfigureWebMetrics(metrics, meterName));
    }

    private static void ConfigureWebTracing(TracerProviderBuilder tracing, string activitySourceName)
    {
        // RecordException covers exceptions that escape the pipeline entirely. Anything ApiExceptionHandler
        // maps to a response is stopped before the instrumentation sees it, so that handler records the
        // exception on the span itself.
        tracing
            .AddAspNetCoreInstrumentation(options => options.RecordException = true)
            .AddHttpClientInstrumentation(options => options.RecordException = true)
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource(activitySourceName);
    }

    private static void ConfigureWebMetrics(MeterProviderBuilder metrics, string meterName)
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(meterName);
    }
}
