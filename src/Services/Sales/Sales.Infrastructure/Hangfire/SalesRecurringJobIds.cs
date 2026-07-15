namespace Sales.Infrastructure;

/// <summary>
/// Stable recurring job identifiers owned by the Sales service.
/// </summary>
public static class SalesRecurringJobIds
{
    /// <summary>Recurring job identifier for Sales cleanup.</summary>
    public const string MaintenanceCleanup = "sales-cleanup";

    /// <summary>Recurring job identifier for expired order cancellation.</summary>
    public const string CancelExpiredPendingOrders = "orders:cancel-expired";
}
