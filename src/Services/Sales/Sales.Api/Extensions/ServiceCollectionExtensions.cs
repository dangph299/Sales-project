using System.Text.Json.Serialization;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Observability;
using BuildingBlocks.Web;
using BuildingBlocks.Web.ExceptionHandling;
using BuildingBlocks.Web.Models;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Sales.Api.Middleware;
using Sales.Application;
using Sales.Infrastructure;

namespace Sales.Api.Extensions;

/// <summary>
/// Composition extensions for the Sales API host.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string ServiceName = "sales-api";

    /// <summary>
    /// Registers all services required by the Sales API host.
    /// </summary>
    /// <param name="builder">Sales API web application builder.</param>
    /// <returns>Builder for chaining.</returns>
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.AddBuildingBlocksLogging(ServiceName);

        builder.Services.AddBuildingBlocksWeb(builder.Configuration, options =>
        {
            options.ServiceName = ServiceName;
            options.ApiTitle = "Sales API";
            options.ApiDescription = "Sales service API for authentication, products, customers, and orders.";
            options.ActivitySourceName = SalesObservability.KafkaActivitySourceName;
            options.MeterName = "Sales.Infrastructure";
            options.JwtClockSkew = TimeSpan.FromSeconds(30);
            options.ConfigureExceptions = ConfigureSalesExceptions;
            options.ConfigureControllers = controllers => controllers.AddJsonOptions(json =>
                json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        });

        builder.Services.AddSingleton<IErrorMessageProvider, SalesErrorMessageProvider>();
        builder.Services.AddSalesApplication();
        builder.Services.AddSalesInfrastructure(builder.Configuration);
        builder.Services.AddSalesBackgroundJobs(builder.Configuration);
        builder.Services.AddSalesIdentity();

        return builder;
    }

    private static void ConfigureSalesExceptions(ApiExceptionHandlingOptions options)
    {
        options.Map<NotFoundException>((_, errorCatalog) =>
        {
            var error = errorCatalog.Get(ErrorCodes.NotFound);
            return new ApiExceptionMapping(404, error.Code, error.Description);
        });

        options.Map<ConflictException>((exception, errorCatalog) =>
        {
            var error = errorCatalog.Get(ErrorCodes.ConcurrencyConflict);
            var errors = new[] { new ApiError("current_version", exception.CurrentVersion.ToString()) };
            return new ApiExceptionMapping(409, error.Code, error.Description, errors);
        });
    }

    private static IServiceCollection AddSalesBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => config.UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(configuration.GetConnectionString("Hangfire"))));
        services.AddHangfireServer(options =>
        {
            options.Queues =
            [
                HangfireQueueNames.Critical,
                HangfireQueueNames.Default,
                HangfireQueueNames.Maintenance
            ];
        });
        return services;
    }

    private static IServiceCollection AddSalesIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SalesDbContext>();

        return services;
    }
}
