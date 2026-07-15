using System.Text.Json.Serialization;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure.Observability.Logging;
using BuildingBlocks.Web.Authentication;
using BuildingBlocks.Web.ExceptionHandling;
using BuildingBlocks.Web.Extensions;
using BuildingBlocks.Web.Models;
using BuildingBlocks.Web.Observability;
using BuildingBlocks.Web.OpenApi;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Sales.Api.Middleware;
using Sales.Application;
using Sales.Domain;
using Sales.Infrastructure;
using Serilog;

namespace Sales.Api.Extensions;

/// <summary>
/// Composition extensions for the Sales API host.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all services required by the Sales API host.
    /// </summary>
    /// <param name="builder">Sales API web application builder.</param>
    /// <returns>Builder for chaining.</returns>
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, config) =>
            config.ConfigureSharedSinks(context.Configuration, "sales-api"));

        builder.Services.AddProblemDetails();
        builder.Services.AddApiExceptionHandling(options =>
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
        });
        builder.Services.AddSingleton<IErrorMessageProvider, SalesErrorMessageProvider>();
        builder.Services.AddSingleton<IErrorCatalog, ErrorCatalogResolver>();
        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddSharedApiModelResponses();
        builder.Services.AddApiDocumentation(
            "Sales API",
            "Sales service API for authentication, products, customers, and orders.");
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateProduct>());
        builder.Services.AddSalesApplication();
        builder.Services.AddSalesInfrastructure(builder.Configuration);
        builder.Services.AddSalesBackgroundJobs(builder.Configuration);
        builder.Services.AddSalesIdentity();
        builder.Services.AddJwtAuthentication(builder.Configuration, TimeSpan.FromSeconds(30));
        builder.Services.AddAuthorization();
        builder.Services.AddApplicationObservability(
            builder.Configuration,
            SalesObservability.KafkaActivitySourceName,
            "Sales.Infrastructure");

        return builder;
    }

    private static IServiceCollection AddSalesBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => config.UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(configuration.GetConnectionString("Hangfire"))));
        services.AddHangfireServer(options => options.Queues = ["critical", "default", "maintenance"]);
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
