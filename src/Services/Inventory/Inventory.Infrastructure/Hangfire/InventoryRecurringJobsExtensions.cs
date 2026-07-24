using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure;

/// <summary>
/// Inventory recurring job registration.
/// </summary>
public static class InventoryRecurringJobsExtensions
{
    public static IServiceCollection AddInventoryRecurringJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<InventoryRecurringJobsOptions>()
            .Bind(configuration.GetSection(InventoryRecurringJobsOptions.SectionName))
            .PostConfigure(options =>
            {
                options.KafkaLagMonitor.Topics = options.KafkaLagMonitor.Topics
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
            })
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<InventoryRecurringJobsOptions>, InventoryRecurringJobsOptionsValidator>());

        services.AddScoped<ReplayDeadLetterJob>();
        services.AddScoped<KafkaLagMonitorJob>();
        services.AddScoped<InboxCleanupJob>();
        services.AddScoped<FailedOutboxRetryJob>();
        services.AddScoped<OutboxPendingMonitorJob>();

        return services;
    }

    /// <summary>
    /// Declares every Inventory recurring job. Registration mechanics belong to
    /// <see cref="RecurringJobManagerExtensions.ScheduleRecurringJob"/>.
    /// </summary>
    public static void RegisterInventoryRecurringJobs(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var serviceScope = serviceProvider.CreateScope();
        var recurringJobManager = serviceScope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var jobsOptions = serviceScope.ServiceProvider.GetRequiredService<IOptions<InventoryRecurringJobsOptions>>().Value;

        recurringJobManager.ScheduleRecurringJob<ReplayDeadLetterJob>(
            InventoryRecurringJobIds.ReplayDeadLetter,
            jobsOptions.ReplayDeadLetter.Schedule,
            replayDeadLetterJob => replayDeadLetterJob.ExecuteAsync(CancellationToken.None));

        recurringJobManager.ScheduleRecurringJob<KafkaLagMonitorJob>(
            InventoryRecurringJobIds.KafkaLagMonitor,
            jobsOptions.KafkaLagMonitor.Schedule,
            kafkaLagMonitorJob => kafkaLagMonitorJob.ExecuteAsync(CancellationToken.None));

        recurringJobManager.ScheduleRecurringJob<InboxCleanupJob>(
            InventoryRecurringJobIds.InboxCleanup,
            jobsOptions.InboxCleanup.Schedule,
            inboxCleanupJob => inboxCleanupJob.ExecuteAsync(CancellationToken.None));

        recurringJobManager.ScheduleRecurringJob<FailedOutboxRetryJob>(
            InventoryRecurringJobIds.FailedOutboxRetry,
            jobsOptions.FailedOutboxRetry.Schedule,
            failedOutboxRetryJob => failedOutboxRetryJob.ExecuteAsync(CancellationToken.None));

        recurringJobManager.ScheduleRecurringJob<OutboxPendingMonitorJob>(
            InventoryRecurringJobIds.OutboxPendingMonitor,
            jobsOptions.OutboxPendingMonitor.Schedule,
            outboxPendingMonitorJob => outboxPendingMonitorJob.ExecuteAsync(CancellationToken.None));
    }
}
