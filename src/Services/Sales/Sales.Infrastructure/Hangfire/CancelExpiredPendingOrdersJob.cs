using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Sales.Application;

namespace Sales.Infrastructure;

/// <summary>
/// Hangfire adapter that dispatches expired open order cancellation.
/// </summary>
public sealed class CancelExpiredPendingOrdersJob(
    ISender sender,
    IClock clock,
    ILogger<CancelExpiredPendingOrdersJob> logger)
{
    /// <summary>Recurring Hangfire job identifier.</summary>
    public const string JobId = SalesRecurringJobIds.CancelExpiredPendingOrders;

    /// <summary>
    /// Executes one expired order cancellation batch.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
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

        logger.LogInformation(
            "CancelExpiredPendingOrdersJob completed {ScannedOrderCount} {CancelledOrderCount} {SkippedOrderCount} {FailedOrderCount} {ElapsedMs}",
            result.ScannedOrderCount,
            result.CancelledOrderCount,
            result.SkippedOrderCount,
            result.FailedOrderCount,
            stopwatch.ElapsedMilliseconds);
    }
}
