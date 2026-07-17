using BuildingBlocks.Infrastructure;

namespace Sales.Infrastructure;

/// <summary>
/// Root configuration for every Sales recurring job. One property per job, so adding a job does not
/// widen this class into a flat list of per-job keys.
/// </summary>
public sealed class SalesRecurringJobsOptions
{
    public const string SectionName = "SalesRecurringJobs";

    /// <summary>Inbox/Outbox cleanup has no business parameters, so it needs schedule settings only.</summary>
    public RecurringJobSettings MaintenanceCleanup { get; init; } = new();

    public CancelExpiredPendingOrdersJobOptions CancelExpiredPendingOrders { get; init; } = new();
}
