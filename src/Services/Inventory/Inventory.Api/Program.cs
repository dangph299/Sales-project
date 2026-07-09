using System.Security.Claims;
using System.Text;
using Inventory.Application;
using Inventory.Infrastructure;
using KafkaFlow;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using BuildingBlocks.Observability;
using BuildingBlocks.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, config) => config.ConfigureSharedSinks(context.Configuration, "inventory-api"));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddInventoryInfrastructure(builder.Configuration);
var key = builder.Configuration["Jwt:Key"] ?? "development-only-key-change-me-32-chars";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new()
{
    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "sales-api",
    ValidAudience = builder.Configuration["Jwt:Audience"] ?? "sales-clients",
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
});
builder.Services.AddAuthorization();
builder.Services.AddOpenTelemetry()
    .WithTracing(x => x.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddEntityFrameworkCoreInstrumentation()
        .AddSource("Inventory.Infrastructure.Kafka").AddOtlpExporter())
    .WithMetrics(x => x.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation()
        .AddMeter("Inventory.Infrastructure").AddOtlpExporter());

var app = builder.Build();
app.UseSerilogRequestLogging(RequestLoggingDefaults.Configure);
app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

var inventory = app.MapGroup("/api/inventory").RequireAuthorization();
inventory.MapGet("/{productId:guid}", async (Guid productId, IInventoryService service, CancellationToken ct) =>
{
    var item = await service.GetAsync(productId, ct);
    return item is null ? Results.NotFound() : Results.Ok(new { item.ProductId, item.Sku, item.Available, item.Reserved, item.Version });
});
inventory.MapGet("/reservations/{orderId:guid}", async (Guid orderId, IInventoryService service, CancellationToken ct) =>
{
    var reservation = await service.GetReservationAsync(orderId, ct);
    return reservation is null ? Results.NotFound() : Results.Ok(reservation);
});
inventory.MapPost("/{productId:guid}/adjust", async (Guid productId, AdjustStockRequest body, HttpContext http, IInventoryService service, CancellationToken ct) =>
{
    var actor = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
    var item = await service.AdjustAsync(productId, body.Sku, body.QuantityDelta, actor, ct);
    return Results.Ok(new { item.ProductId, item.Sku, item.Available, item.Reserved, item.Version });
}).RequireAuthorization(r => r.RequireRole("Admin", "Warehouse"));

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
var bus = app.Services.CreateKafkaBus();
await bus.StartAsync();
app.Lifetime.ApplicationStopping.Register(() => bus.StopAsync().GetAwaiter().GetResult());
app.Run();

public partial class Program;
