using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

public sealed class CancelExpiredPendingOrdersJobDefinition : RecurringJobDefinitionBase
{
    private readonly CancelExpiredPendingOrdersJobOptions jobOptions;

    public CancelExpiredPendingOrdersJobDefinition(
        IRecurringJobManager recurringJobManager,
        IOptions<SalesRecurringJobsOptions> salesRecurringJobsOptions)
        : base(recurringJobManager, salesRecurringJobsOptions.Value.CancelExpiredPendingOrders.Schedule)
    {
        jobOptions = salesRecurringJobsOptions.Value.CancelExpiredPendingOrders;
    }

    protected override string JobId
    {
        get
        {
            return SalesRecurringJobIds.CancelExpiredPendingOrders;
        }
    }

    protected override void AddOrUpdate()
    {
        RecurringJobManager.AddOrUpdateRecurringJob<CancelExpiredPendingOrdersJob>(
            SalesRecurringJobIds.CancelExpiredPendingOrders,
            Settings.Queue,
            Settings.Cron,
            cancelExpiredPendingOrdersJob => cancelExpiredPendingOrdersJob.ExecuteAsync(
                jobOptions.ExpirationMinutes,
                jobOptions.BatchSize,
                CancellationToken.None));
    }
}
