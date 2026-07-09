using Microsoft.Extensions.Configuration;
using Serilog;

namespace BuildingBlocks.Observability;

/// <summary>
/// Shared Serilog bootstrap used by every service instead of a per-service copy.
/// </summary>
public static class SerilogBootstrap
{
    /// <summary>
    /// One shared sink/enricher policy for every service: Console + Seq for human triage,
    /// OTLP so logs join the same trace/span as the existing traces+metrics pipeline in Kibana.
    /// </summary>
    /// <param name="config">
    /// The logger configuration to extend.
    /// </param>
    /// <param name="configuration">
    /// The application configuration, used for service name, environment, OTLP endpoint, and Seq URL.
    /// </param>
    /// <param name="defaultServiceName">
    /// The service name to use if <c>OTEL_SERVICE_NAME</c> is not configured.
    /// </param>
    /// <returns>
    /// The logger configuration, to allow chaining.
    /// </returns>
    public static LoggerConfiguration ConfigureSharedSinks(this LoggerConfiguration config, IConfiguration configuration, string defaultServiceName)
    {
        var serviceName = configuration["OTEL_SERVICE_NAME"] ?? defaultServiceName;
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"] ?? "Production";
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";

        return config
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.WithProperty("Environment", environment)
            .WriteTo.Console()
            .WriteTo.Seq(configuration["Seq:Url"] ?? "http://seq:5341")
            .WriteTo.OpenTelemetry(otel =>
            {
                otel.Endpoint = otlpEndpoint;
                otel.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                otel.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = serviceName };
            });
    }
}
