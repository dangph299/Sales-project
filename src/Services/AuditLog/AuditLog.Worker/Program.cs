using AuditLog.Worker;
using BuildingBlocks.Observability;

var builder = Host.CreateApplicationBuilder(args);

builder.AddBuildingBlocksLogging("audit-worker");
builder.Services.AddBuildingBlocksObservability(
    "audit-worker",
    tracingSourceNames: ["AuditLog.Infrastructure.Kafka"]);
builder.Services.AddAuditLogWorker(builder.Configuration);

await builder.Build().RunAsync();
