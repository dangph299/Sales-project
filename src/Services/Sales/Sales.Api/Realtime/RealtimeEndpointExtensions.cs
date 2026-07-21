namespace Sales.Api.Realtime;

/// <summary>
/// Maps Sales realtime endpoints.
/// </summary>
public static class RealtimeEndpointExtensions
{
    /// <summary>
    /// Maps the order SignalR hub.
    /// </summary>
    public static WebApplication MapSalesRealtime(this WebApplication app)
    {
        app.MapHub<OrderHub>("/hubs/orders");
        return app;
    }
}

