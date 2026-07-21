namespace Sales.Api.Realtime;

internal static class OrderRealtimeGroups
{
    internal const string OrderList = "orders:list";

    internal static string ForOrder(Guid orderId)
    {
        return $"order:{orderId:N}";
    }
}

