using System.Net;
using Dashboard.Bff.Clients;

namespace Dashboard.Bff.Tests;

/// <summary>
/// Exercises <see cref="InventoryClient"/>: outgoing request shape and response mapping.
/// </summary>
public sealed class InventoryClientTests
{
    private const string SummaryJson =
        """
        {
          "success": true,
          "message": null,
          "correlationId": "11111111-1111-1111-1111-111111111111",
          "data": {
            "totalItems": 100,
            "totalQuantity": 5000,
            "inStock": 80,
            "lowStock": 15,
            "outOfStock": 5,
            "lowStockThreshold": 10
          }
        }
        """;

    private static InventoryClient CreateClient(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://inventory.local") };
        return new InventoryClient(httpClient);
    }

    [Fact]
    public async Task GetInventorySummaryAsync_requests_correct_path_and_maps_result()
    {
        var handler = new FakeHttpMessageHandler(SummaryJson);
        var client = CreateClient(handler);

        var result = await client.GetInventorySummaryAsync(10, CancellationToken.None);

        Assert.Equal("/api/inventory/summary", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("lowStockThreshold=10", handler.LastRequest.RequestUri.Query.TrimStart('?'));

        Assert.Equal(100, result.TotalItems);
        Assert.Equal(5000, result.TotalQuantity);
        Assert.Equal(80, result.InStock);
        Assert.Equal(15, result.LowStock);
        Assert.Equal(5, result.OutOfStock);
        Assert.Equal(10, result.LowStockThreshold);
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
