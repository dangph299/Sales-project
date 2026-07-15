using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

namespace BuildingBlocks.Observability;

/// <summary>
/// Capability registration for the shared Serilog logging and OpenTelemetry tracing/metrics
/// pipelines, so no service repeats sink, exporter, or runtime-instrumentation wiring.
/// </summary>
public static class ObservabilityRegistration
{
    /// <summary>
    /// Wires Serilog as the host's logging provider using the solution's shared sink policy.
    /// Works for both Web API and Worker hosts.
    /// </summary>
    /// <typeparam name="TBuilder">Host application builder type.</typeparam>
    /// <param name="builder">Host application builder.</param>
    /// <param name="serviceName">Service name used when <c>OTEL_SERVICE_NAME</c> is not configured.</param>
    /// <returns>The same builder for chaining.</returns>
    public static TBuilder AddBuildingBlocksLogging<TBuilder>(this TBuilder builder, string serviceName)
        where TBuilder : IHostApplicationBuilder
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("A service name is required for logging.", nameof(serviceName));
        }

        builder.Services.AddSerilog((_, loggerConfiguration) =>
            loggerConfiguration.ConfigureSharedSinks(builder.Configuration, serviceName));
        return builder;
    }

    /// <summary>
    /// Registers the base OpenTelemetry pipeline for hosts that only need to name their own
    /// activity sources and meters (for example, workers). The caller does not reference any
    /// OpenTelemetry type.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="serviceName">Service name identifying the host; the resource value is resolved from OTEL_SERVICE_NAME.</param>
    /// <param name="tracingSourceNames">Activity source names to include in tracing.</param>
    /// <param name="meterNames">Meter names to include in metrics.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddBuildingBlocksObservability(
        this IServiceCollection services,
        string serviceName,
        IReadOnlyCollection<string>? tracingSourceNames = null,
        IReadOnlyCollection<string>? meterNames = null)
    {
        return services.AddBuildingBlocksObservability(
            serviceName,
            tracing =>
            {
                foreach (var source in tracingSourceNames ?? [])
                {
                    tracing.AddSource(source);
                }
            },
            metrics =>
            {
                foreach (var meter in meterNames ?? [])
                {
                    metrics.AddMeter(meter);
                }
            });
    }

    /// <summary>
    /// Registers the base OpenTelemetry pipeline shared by every host: OTLP export on traces and
    /// metrics plus runtime instrumentation. Callers add their own instrumentation and
    /// activity/meter sources through the configuration callbacks.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="serviceName">Service name identifying the host; the resource value is resolved from OTEL_SERVICE_NAME.</param>
    /// <param name="configureTracing">Hook to add service-specific tracing instrumentation and sources.</param>
    /// <param name="configureMetrics">Hook to add service-specific metrics instrumentation and meters.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddBuildingBlocksObservability(
        this IServiceCollection services,
        string serviceName,
        Action<TracerProviderBuilder>? configureTracing,
        Action<MeterProviderBuilder>? configureMetrics)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("A service name is required for observability.", nameof(serviceName));
        }

        // The OpenTelemetry resource service name is resolved by the SDK from OTEL_SERVICE_NAME,
        // matching how the shared Serilog sinks name the service. serviceName is validated above
        // so every host declares its identity explicitly.
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                configureTracing?.Invoke(tracing);
                tracing.AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation();
                configureMetrics?.Invoke(metrics);
                metrics.AddOtlpExporter();
            });

        return services;
    }
}
