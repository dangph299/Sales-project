namespace Sales.Application.Features.Orders.Commands;

/// <summary>
/// Summary of one expired open order cancellation batch.
/// </summary>
/// <param name="ScannedOrderCount">Number of candidate orders scanned.</param>
/// <param name="CancelledOrderCount">Number of orders cancelled.</param>
/// <param name="SkippedOrderCount">Number of candidates that no longer qualified.</param>
/// <param name="FailedOrderCount">Number of candidates that failed during processing.</param>
public sealed record CancelExpiredPendingOrdersResult(
    int ScannedOrderCount,
    int CancelledOrderCount,
    int SkippedOrderCount,
    int FailedOrderCount);
