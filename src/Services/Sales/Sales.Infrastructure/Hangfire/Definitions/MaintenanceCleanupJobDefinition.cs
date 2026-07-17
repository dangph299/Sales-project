using BuildingBlocks.Infrastructure;

using Hangfire;

using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

public sealed class MaintenanceCleanupJobDefinition : RecurringJobDefinitionBase
{
    public MaintenanceCleanupJobDefinition(
        IRecurringJobManager recurringJobManager,
        IOptions<SalesRecurringJobsOptions> salesRecurringJobsOptions)
        : base(recurringJobManager, salesRecurringJobsOptions.Value.MaintenanceCleanup)
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
        RecurringJobManager.AddOrUpdateRecurringJob<MaintenanceCleanupJob>(
            SalesRecurringJobIds.MaintenanceCleanup,
            Settings.Queue,
            Settings.Cron,
            maintenanceCleanupJob => maintenanceCleanupJob.CleanupAsync());
    }
}
