using Microsoft.AspNetCore.SignalR;
using Sales.Application.Features.Orders.Realtime;

namespace Sales.Api.Realtime;

internal sealed class SignalROrderRealtimeNotifier(
    IHubContext<OrderHub> hubContext) : IOrderRealtimeNotifier
{
    public async Task NotifyOrderStatusChangedAsync(
        OrderStatusChangedNotification notification,
        CancellationToken cancellationToken)
    {
        await hubContext.Clients
            .Group(OrderRealtimeGroups.ForOrder(notification.OrderId))
            .SendAsync(OrderRealtimeEvents.StatusChanged, notification, cancellationToken);

        await hubContext.Clients
            .Group(OrderRealtimeGroups.OrderList)
            .SendAsync(OrderRealtimeEvents.StatusChanged, notification, cancellationToken);
    }
}

