using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Sales.Api.Realtime;

/// <summary>
/// SignalR endpoint for subscribing to order change notifications.
/// </summary>
[Authorize(Roles = "Admin,Sales")]
public sealed class OrderHub : Hub
{
    /// <summary>
    /// Adds the current connection to the group for one order.
    /// </summary>
    public Task SubscribeToOrder(Guid orderId)
    {
        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            OrderRealtimeGroups.ForOrder(orderId));
    }

    /// <summary>
    /// Removes the current connection from the group for one order.
    /// </summary>
    public Task UnsubscribeFromOrder(Guid orderId)
    {
        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            OrderRealtimeGroups.ForOrder(orderId));
    }

    /// <summary>
    /// Adds the current connection to the order list notification group.
    /// </summary>
    public Task SubscribeToOrderList()
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, OrderRealtimeGroups.OrderList);
    }

    /// <summary>
    /// Removes the current connection from the order list notification group.
    /// </summary>
    public Task UnsubscribeFromOrderList()
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, OrderRealtimeGroups.OrderList);
    }
}

