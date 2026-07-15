using BuildingBlocks.Infrastructure;

using Hangfire;

using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

public sealed class MaintenanceCleanupRecurringJobRegistration
    : RecurringJobRegistrationBase<RecurringJobScheduleOptions>
{
    public const string OptionsName = "Sales.MaintenanceCleanup";

    public const string SectionPath = "SalesRecurringJobs:MaintenanceCleanup";

    public MaintenanceCleanupRecurringJobRegistration(
        IRecurringJobManager recurringJobManager,
        IOptionsMonitor<RecurringJobScheduleOptions> options)
        : base(recurringJobManager, options.Get(OptionsName))
    {
    }

    protected override string JobId
    {
        get
        {
            return SalesRecurringJobIds.MaintenanceCleanup;
        }
    }

    protected override void AddOrUpdate()
    {
        RecurringJobManager.AddOrUpdateRecurringJob<MaintenanceJobs>(
            SalesRecurringJobIds.MaintenanceCleanup,
            HangfireQueueNames.Maintenance,
            Options.Cron,
            maintenanceJobs => maintenanceJobs.CleanupAsync());
    }
}
