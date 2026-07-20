using BuildingBlocks.Infrastructure;
using Hangfire;
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

        return services;
    }

    /// <summary>
    /// Declares every Sales recurring job. Registration mechanics (queue, cron, UTC options, and the
    /// enabled/disabled flow) belong to <see cref="RecurringJobManagerExtensions.ScheduleRecurringJob"/>.
    /// </summary>
    public static void RegisterSalesRecurringJobs(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var serviceScope = serviceProvider.CreateScope();
        var recurringJobManager = serviceScope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var jobsOptions = serviceScope.ServiceProvider.GetRequiredService<IOptions<SalesRecurringJobsOptions>>().Value;

        recurringJobManager.ScheduleRecurringJob<MaintenanceCleanupJob>(
            SalesRecurringJobIds.MaintenanceCleanup,
            jobsOptions.MaintenanceCleanup,
            maintenanceCleanupJob => maintenanceCleanupJob.CleanupAsync());

        var cancelExpiredPendingOrders = jobsOptions.CancelExpiredPendingOrders;
        recurringJobManager.ScheduleRecurringJob<CancelExpiredPendingOrdersJob>(
            SalesRecurringJobIds.CancelExpiredPendingOrders,
            cancelExpiredPendingOrders.Schedule,
            cancelExpiredPendingOrdersJob => cancelExpiredPendingOrdersJob.ExecuteAsync(
                cancelExpiredPendingOrders.ExpirationMinutes,
                cancelExpiredPendingOrders.BatchSize,
                CancellationToken.None));
    }
}
