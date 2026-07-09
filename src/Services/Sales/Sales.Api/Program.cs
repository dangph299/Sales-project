using System.Text;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sales.Application;
using Sales.Domain;
using Sales.Infrastructure;
using Serilog;
using KafkaFlow;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Dashboard;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Sales.Api.Extensions;
using Sales.Api.Filters;
using BuildingBlocks.Observability;
using BuildingBlocks.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, config) => config.ConfigureSharedSinks(context.Configuration, "sales-api"));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExceptionHandlingMiddleware>();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sales API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste the JWT accessToken returned by POST /api/auth/login.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.OperationFilter<AuthorizeOperationFilter>();
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateProduct>());
builder.Services.AddSalesApplication();
builder.Services.AddSalesInfrastructure(builder.Configuration);
builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(options =>
    options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Hangfire"))));
builder.Services.AddHangfireServer(options => options.Queues = ["critical", "default", "maintenance"]);
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<SalesDbContext>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "development-only-key-change-me-32-chars";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new()
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "sales-api",
    ValidAudience = builder.Configuration["Jwt:Audience"] ?? "sales-clients",
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
    ClockSkew = TimeSpan.FromSeconds(30)
});
builder.Services.AddAuthorization();
builder.Services.AddOpenTelemetry()
    .WithTracing(x => x.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddEntityFrameworkCoreInstrumentation()
        .AddSource("Sales.Infrastructure.Kafka").AddOtlpExporter())
    .WithMetrics(x => x.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation()
        .AddMeter("Sales.Infrastructure").AddOtlpExporter());

var app = builder.Build();
var kafkaBus = app.Services.CreateKafkaBus();
await kafkaBus.StartAsync();
app.Lifetime.ApplicationStopping.Register(() => kafkaBus.StopAsync().GetAwaiter().GetResult());
app.UseExceptionHandler();
app.UseSerilogRequestLogging(RequestLoggingDefaults.Configure);
app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new LocalDashboardAuthorizationFilter()]
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sales API v1");
        options.RoutePrefix = "swagger";
    });
}
app.MapControllers();

await app.Services.SeedIdentityAsync();
RecurringJob.AddOrUpdate<MaintenanceJobs>("sales-cleanup", "maintenance", x => x.CleanupAsync(), Cron.Daily);
app.Run();

public partial class Program;
