using AuditLog.Worker;
using BuildingBlocks.Infrastructure.Observability.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((_, loggerConfig) => loggerConfig.ConfigureSharedSinks(builder.Configuration, "audit-worker"));

builder.Services.AddAuditLogWorker(builder.Configuration);
builder.Services.AddOpenTelemetry()
    .WithTracing(x => x.AddSource("AuditLog.Infrastructure.Kafka").AddOtlpExporter())
    .WithMetrics(x => x.AddRuntimeInstrumentation().AddOtlpExporter());
await builder.Build().RunAsync();
