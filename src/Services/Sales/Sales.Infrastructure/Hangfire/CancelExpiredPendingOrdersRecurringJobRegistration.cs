using BuildingBlocks.Infrastructure;
using Hangfire;
using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

public sealed class CancelExpiredPendingOrdersRecurringJobRegistration
    : RecurringJobRegistrationBase<CancelExpiredPendingOrdersJobOptions>
{
    public const string SectionPath = "SalesRecurringJobs:CancelExpiredPendingOrders";

    public CancelExpiredPendingOrdersRecurringJobRegistration(
        IRecurringJobManager recurringJobManager,
        IOptions<CancelExpiredPendingOrdersJobOptions> options)
        : base(recurringJobManager, options.Value)
    {
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
            HangfireQueueNames.Maintenance,
            Options.Cron,
            cancelExpiredPendingOrdersJob => cancelExpiredPendingOrdersJob.ExecuteAsync(
                Options.ExpirationMinutes,
                Options.BatchSize,
                CancellationToken.None));
    }
}
