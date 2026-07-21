using Sales.Domain;

namespace Sales.Application.Features.Orders.Realtime;

/// <summary>
/// Publishes best-effort realtime notifications when an order's persisted status changes.
/// </summary>
public interface IOrderRealtimeNotifier
{
    /// <summary>
    /// Notifies connected clients that an order status changed.
    /// </summary>
    Task NotifyOrderStatusChangedAsync(
        OrderStatusChangedNotification notification,
        CancellationToken cancellationToken);
}

