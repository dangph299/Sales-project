using BuildingBlocks.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sales.Infrastructure;

public static class SalesRecurringJobsExtensions
{
    public static IServiceCollection AddSalesRecurringJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRecurringJobOptions<RecurringJobScheduleOptions>(
            configuration,
            MaintenanceCleanupRecurringJobRegistration.OptionsName,
            MaintenanceCleanupRecurringJobRegistration.SectionPath,
            "Maintenance cleanup recurring job configuration is invalid.");

        services.AddRecurringJobOptions<CancelExpiredPendingOrdersJobOptions>(
            configuration,
            CancelExpiredPendingOrdersRecurringJobRegistration.SectionPath,
            "Cancel expired pending orders recurring job configuration is invalid.");

        services.AddScoped<MaintenanceJobs>();
        services.AddScoped<CancelExpiredPendingOrdersJob>();
        services.AddScoped<IRecurringJobRegistration, MaintenanceCleanupRecurringJobRegistration>();
        services.AddScoped<IRecurringJobRegistration, CancelExpiredPendingOrdersRecurringJobRegistration>();

        return services;
    }
}
