using BuildingBlocks.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

public static class SalesRecurringJobsExtensions
{
    public static IServiceCollection AddSalesRecurringJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SalesRecurringJobsOptions>()
            .Bind(configuration.GetSection(SalesRecurringJobsOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<SalesRecurringJobsOptions>, SalesRecurringJobsOptionsValidator>());

        services.AddScoped<MaintenanceCleanupJob>();
        services.AddScoped<CancelExpiredPendingOrdersJob>();
        services.AddScoped<IRecurringJobDefinition, MaintenanceCleanupJobDefinition>();
        services.AddScoped<IRecurringJobDefinition, CancelExpiredPendingOrdersJobDefinition>();

        return services;
    }
}
