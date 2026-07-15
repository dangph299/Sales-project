namespace Sales.Application;

/// <summary>
/// Cancels open orders that have waited longer than the configured expiration window.
/// </summary>
/// <param name="CurrentUtc">Current UTC time used to calculate expiration.</param>
/// <param name="ExpirationMinutes">Number of minutes an open order may remain unchanged.</param>
/// <param name="BatchSize">Maximum number of candidate orders to scan.</param>
public sealed record CancelExpiredPendingOrders(
    DateTimeOffset CurrentUtc,
    int ExpirationMinutes,
    int BatchSize) : ICommand<CancelExpiredPendingOrdersResult>
{
    /// <summary>
    /// Stable logical name of the maintenance job that issues this command, used for log correlation
    /// independently of the Hangfire recurring-job identifier declared in Infrastructure.
    /// </summary>
    public const string JobName = "CancelExpiredPendingOrders";
}
