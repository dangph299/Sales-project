using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles automatic cancellation of expired open orders.
/// </summary>
public sealed class CancelExpiredPendingOrdersHandler(
    IOrderRepository orderRepository,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<CancelExpiredPendingOrdersHandler> logger) : IRequestHandler<CancelExpiredPendingOrders, CancelExpiredPendingOrdersResult>
{
    private const int MaximumBatchSize = 1_000;

    /// <inheritdoc/>
    public async Task<CancelExpiredPendingOrdersResult> Handle(
        CancelExpiredPendingOrders request,
        CancellationToken cancellationToken)
    {
        var effectiveBatchSize = Math.Clamp(request.BatchSize, 1, MaximumBatchSize);
        var orderUpdatedBefore = request.CurrentUtc.AddMinutes(-request.ExpirationMinutes);

        var expiredCancellableOrderIds = await orderRepository.FindExpiredCancellableOrderIdsAsync(
            orderUpdatedBefore,
            effectiveBatchSize,
            cancellationToken);

        var cancelledOrderCount = 0;
        var skippedOrderCount = 0;
        var failedOrderCount = 0;

        foreach (var orderId in expiredCancellableOrderIds)
        {
            try
            {
                var wasOrderCancelled = await CancelOneOrder(
                    orderId,
                    orderUpdatedBefore,
                    cancellationToken);

                if (wasOrderCancelled)
                {
                    cancelledOrderCount++;
                }
                else
                {
                    skippedOrderCount++;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failedOrderCount++;
                logger.LogWarning(
                    exception,
                    "Expired order cancellation failed {OrderId}",
                    orderId);
            }
        }

        return new CancelExpiredPendingOrdersResult(
            expiredCancellableOrderIds.Count,
            cancelledOrderCount,
            skippedOrderCount,
            failedOrderCount);
    }

    private async Task<bool> CancelOneOrder(
        Guid orderId,
        DateTimeOffset orderUpdatedBefore,
        CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var scopedOrderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var order = await scopedOrderRepository.GetWithLinesAsync(orderId, cancellationToken);
        if (order is null)
        {
            return false;
        }

        var wasOrderCancelled = order.CancelDueToExpiration(orderUpdatedBefore);
        if (!wasOrderCancelled)
        {
            return false;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
