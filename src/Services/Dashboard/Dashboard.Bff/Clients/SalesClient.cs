using System.Net.Http.Json;
using System.Text.Json;
using Dashboard.Bff.Clients.Payloads;
using Dashboard.Bff.Contracts;

namespace Dashboard.Bff.Clients;

/// <summary>
/// Typed client for reading order, product, and customer data from Sales.Api.
/// </summary>
public sealed class SalesClient : ISalesClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public SalesClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<RecentOrderDto>> GetRecentOrdersAsync(int take, CancellationToken cancellationToken)
    {
        var page = await GetOrdersPageAsync($"/api/orders?page=1&pageSize={take}", cancellationToken);
        return page.Items.Select(ToRecentOrderDto).ToList();
    }

    public async Task<IReadOnlyList<OrderChartPointDto>> GetOrdersInRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var query = $"/api/orders?from={Uri.EscapeDataString(from.ToString("o"))}" +
            $"&to={Uri.EscapeDataString(to.ToString("o"))}&page=1&pageSize=100";
        var page = await GetOrdersPageAsync(query, cancellationToken);
        return page.Items.Select(ToOrderChartPointDto).ToList();
    }

    public Task<long> GetOrderCountAsync(CancellationToken cancellationToken) =>
        GetTotalAsync("/api/orders?page=1&pageSize=1", cancellationToken);

    public Task<long> GetPendingOrderCountAsync(CancellationToken cancellationToken) =>
        GetTotalAsync("/api/orders?status=PendingInventory&page=1&pageSize=1", cancellationToken);

    public Task<long> GetProductCountAsync(CancellationToken cancellationToken) =>
        GetTotalAsync("/api/products?page=1&pageSize=1", cancellationToken);

    public Task<long> GetPublishedProductCountAsync(CancellationToken cancellationToken) =>
        GetTotalAsync("/api/products?status=Published&page=1&pageSize=1", cancellationToken);

    public Task<long> GetCustomerCountAsync(CancellationToken cancellationToken) =>
        GetTotalAsync("/api/customers?page=1&pageSize=1", cancellationToken);

    private async Task<PagedResultPayload<OrderListItemPayload>> GetOrdersPageAsync(
        string requestUri,
        CancellationToken cancellationToken)
    {
        var envelope = await _httpClient.GetFromJsonAsync<ApiEnvelope<PagedResultPayload<OrderListItemPayload>>>(
            requestUri, JsonOptions, cancellationToken);

        return envelope?.Data
            ?? throw new InvalidOperationException($"Sales.Api returned an empty response for {requestUri}.");
    }

    private async Task<long> GetTotalAsync(string requestUri, CancellationToken cancellationToken)
    {
        var envelope = await _httpClient.GetFromJsonAsync<ApiEnvelope<PagedResultPayload<object>>>(
            requestUri, JsonOptions, cancellationToken);

        return envelope?.Data?.Total
            ?? throw new InvalidOperationException($"Sales.Api returned an empty response for {requestUri}.");
    }

    private static RecentOrderDto ToRecentOrderDto(OrderListItemPayload item) => new(
        item.Id,
        item.OrderCode,
        item.CustomerName,
        item.Status,
        item.TotalQuantity,
        item.Total,
        item.CreatedAt);

    private static OrderChartPointDto ToOrderChartPointDto(OrderListItemPayload item) => new(
        item.CreatedAt,
        item.Total,
        item.Status);
}
