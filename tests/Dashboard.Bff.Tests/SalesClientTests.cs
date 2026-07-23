using System.Net;
using Dashboard.Bff.Clients;

namespace Dashboard.Bff.Tests;

/// <summary>
/// Exercises <see cref="SalesClient"/>: outgoing request shape and response mapping.
/// </summary>
public sealed class SalesClientTests
{
    private const string OrdersEnvelopeJson =
        """
        {
          "success": true,
          "message": null,
          "correlationId": "11111111-1111-1111-1111-111111111111",
          "data": {
            "items": [
              {
                "id": "22222222-2222-2222-2222-222222222222",
                "orderCode": "ORD-0001",
                "customerName": "Alice",
                "status": "PendingInventory",
                "totalQuantity": 3,
                "total": 123.45,
                "createdAt": "2026-07-20T10:00:00Z"
              }
            ],
            "page": 1,
            "pageSize": 5,
            "total": 1
          }
        }
        """;

    private static string CountEnvelopeJson(long total) =>
        $$"""
        {
          "success": true,
          "message": null,
          "correlationId": "11111111-1111-1111-1111-111111111111",
          "data": { "items": [], "page": 1, "pageSize": 1, "total": {{total}} }
        }
        """;

    private static SalesClient CreateClient(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://sales.local") };
        return new SalesClient(httpClient);
    }

    [Fact]
    public async Task GetRecentOrdersAsync_requests_correct_path_and_maps_items()
    {
        var handler = new FakeHttpMessageHandler(OrdersEnvelopeJson);
        var client = CreateClient(handler);

        var result = await client.GetRecentOrdersAsync(5, CancellationToken.None);

        Assert.Equal("/api/orders", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("page=1&pageSize=5", handler.LastRequest.RequestUri.Query.TrimStart('?'));

        var order = Assert.Single(result);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), order.Id);
        Assert.Equal("ORD-0001", order.OrderCode);
        Assert.Equal("Alice", order.CustomerName);
        Assert.Equal("PendingInventory", order.Status);
        Assert.Equal(3, order.TotalQuantity);
        Assert.Equal(123.45m, order.Total);
        Assert.Equal(DateTimeOffset.Parse("2026-07-20T10:00:00Z"), order.CreatedAt);
    }

    [Fact]
    public async Task GetOrdersInRangeAsync_requests_correct_path_and_maps_items()
    {
        var handler = new FakeHttpMessageHandler(OrdersEnvelopeJson);
        var client = CreateClient(handler);
        var from = DateTimeOffset.Parse("2026-07-16T00:00:00Z");
        var to = DateTimeOffset.Parse("2026-07-23T00:00:00Z");

        var result = await client.GetOrdersInRangeAsync(from, to, CancellationToken.None);

        Assert.Equal("/api/orders", handler.LastRequest!.RequestUri!.AbsolutePath);
        var query = System.Web.HttpUtility.ParseQueryString(handler.LastRequest.RequestUri.Query);
        Assert.Equal(from, DateTimeOffset.Parse(query["from"]!));
        Assert.Equal(to, DateTimeOffset.Parse(query["to"]!));
        Assert.Equal("1", query["page"]);
        Assert.Equal("100", query["pageSize"]);

        var point = Assert.Single(result);
        Assert.Equal(123.45m, point.Total);
        Assert.Equal("PendingInventory", point.Status);
        Assert.Equal(DateTimeOffset.Parse("2026-07-20T10:00:00Z"), point.CreatedAt);
    }

    [Fact]
    public async Task GetOrderCountAsync_requests_orders_with_pageSize_1_and_returns_total()
    {
        var handler = new FakeHttpMessageHandler(CountEnvelopeJson(42));
        var client = CreateClient(handler);

        var count = await client.GetOrderCountAsync(CancellationToken.None);

        Assert.Equal("/api/orders", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("page=1&pageSize=1", handler.LastRequest.RequestUri.Query.TrimStart('?'));
        Assert.Equal(42, count);
    }

    [Fact]
    public async Task GetPendingOrderCountAsync_requests_orders_with_pending_status_and_returns_total()
    {
        var handler = new FakeHttpMessageHandler(CountEnvelopeJson(7));
        var client = CreateClient(handler);

        var count = await client.GetPendingOrderCountAsync(CancellationToken.None);

        Assert.Equal("/api/orders", handler.LastRequest!.RequestUri!.AbsolutePath);
        var query = System.Web.HttpUtility.ParseQueryString(handler.LastRequest.RequestUri.Query);
        Assert.Equal("PendingInventory", query["status"]);
        Assert.Equal("1", query["page"]);
        Assert.Equal("1", query["pageSize"]);
        Assert.Equal(7, count);
    }

    [Fact]
    public async Task GetProductCountAsync_requests_products_with_pageSize_1_and_returns_total()
    {
        var handler = new FakeHttpMessageHandler(CountEnvelopeJson(15));
        var client = CreateClient(handler);

        var count = await client.GetProductCountAsync(CancellationToken.None);

        Assert.Equal("/api/products", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("page=1&pageSize=1", handler.LastRequest.RequestUri.Query.TrimStart('?'));
        Assert.Equal(15, count);
    }

    [Fact]
    public async Task GetPublishedProductCountAsync_requests_products_with_published_status_and_returns_total()
    {
        var handler = new FakeHttpMessageHandler(CountEnvelopeJson(9));
        var client = CreateClient(handler);

        var count = await client.GetPublishedProductCountAsync(CancellationToken.None);

        Assert.Equal("/api/products", handler.LastRequest!.RequestUri!.AbsolutePath);
        var query = System.Web.HttpUtility.ParseQueryString(handler.LastRequest.RequestUri.Query);
        Assert.Equal("Published", query["status"]);
        Assert.Equal("1", query["page"]);
        Assert.Equal("1", query["pageSize"]);
        Assert.Equal(9, count);
    }

    [Fact]
    public async Task GetCustomerCountAsync_requests_customers_with_pageSize_1_and_returns_total()
    {
        var handler = new FakeHttpMessageHandler(CountEnvelopeJson(3));
        var client = CreateClient(handler);

        var count = await client.GetCustomerCountAsync(CancellationToken.None);

        Assert.Equal("/api/customers", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("page=1&pageSize=1", handler.LastRequest.RequestUri.Query.TrimStart('?'));
        Assert.Equal(3, count);
    }

    /// <summary>Hand-rolled fake HTTP handler recording the last request and returning canned JSON.</summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public FakeHttpMessageHandler(string responseJson) => _responseJson = responseJson;

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
