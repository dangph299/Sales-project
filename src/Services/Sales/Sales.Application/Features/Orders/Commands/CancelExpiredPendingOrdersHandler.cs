using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sales.Application.Features.Orders.Realtime;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Commands;

/// <summary>
/// Handles automatic cancellation of expired open orders.
/// </summary>
public sealed class CancelExpiredPendingOrdersHandler(
    IOrderRepository orderRepository,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<CancelExpiredPendingOrdersHandler> logger) : ICommandHandler<CancelExpiredPendingOrders, CancelExpiredPendingOrdersResult>
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
                    "Expired order cancellation failed {OrderId} {ExpiredBeforeUtc} {JobId}",
                    orderId,
                    orderUpdatedBefore,
                    CancelExpiredPendingOrders.JobName);
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
        var orderRealtimeNotifier = scope.ServiceProvider.GetRequiredService<IOrderRealtimeNotifier>();

        var order = await scopedOrderRepository.GetWithLinesAsync(orderId, cancellationToken);
        if (order is null)
        {
            return false;
        }

        var previousStatus = order.Status;
        var wasOrderCancelled = order.CancelDueToExpiration(orderUpdatedBefore);
        if (!wasOrderCancelled)
        {
            return false;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await NotifyOrderCancelledAfterSave(
            orderRealtimeNotifier,
            order,
            previousStatus,
            cancellationToken);
        return true;
    }

    private async Task NotifyOrderCancelledAfterSave(
        IOrderRealtimeNotifier orderRealtimeNotifier,
        Order order,
        OrderStatus previousStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            await orderRealtimeNotifier.NotifyOrderStatusChangedAsync(
                new OrderStatusChangedNotification(
                    order.Id,
                    previousStatus,
                    order.Status,
                    order.UpdatedAt,
                    order.Version),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Expired order realtime notification failed after save {OrderId} {PreviousStatus} {CurrentStatus}",
                order.Id,
                previousStatus,
                order.Status);
        }
    }
}
