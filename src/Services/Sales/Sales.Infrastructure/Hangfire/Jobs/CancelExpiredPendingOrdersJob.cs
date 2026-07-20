using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Sales.Application.Features.Orders.Commands;

namespace Sales.Infrastructure;

/// <summary>
/// Hangfire adapter that dispatches expired open order cancellation.
/// </summary>
public sealed class CancelExpiredPendingOrdersJob(
    ISender sender,
    IClock clock,
    ILogger<CancelExpiredPendingOrdersJob> logger)
{
    /// <summary>
    /// Executes one expired order cancellation batch.
    /// </summary>
    public async Task ExecuteAsync(
        int expirationMinutes,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("CancelExpiredPendingOrdersJob started");

        var result = await sender.Send(
            new CancelExpiredPendingOrders(
                clock.UtcNow,
                expirationMinutes,
                batchSize),
            cancellationToken);

        SalesMetrics.RecordExpiredOrderCancellation(
            result.ScannedOrderCount,
            result.CancelledOrderCount,
            result.SkippedOrderCount,
            result.FailedOrderCount,
            stopwatch.Elapsed.TotalMilliseconds);

        logger.LogInformation(
            "CancelExpiredPendingOrdersJob completed {ScannedOrderCount} {CancelledOrderCount} {SkippedOrderCount} {FailedOrderCount} {ElapsedMs}",
            result.ScannedOrderCount,
            result.CancelledOrderCount,
            result.SkippedOrderCount,
            result.FailedOrderCount,
            stopwatch.ElapsedMilliseconds);
    }
}
