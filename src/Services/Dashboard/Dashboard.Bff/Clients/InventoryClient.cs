using System.Net.Http.Json;
using System.Text.Json;
using Dashboard.Bff.Clients.Payloads;
using Dashboard.Bff.Contracts;

namespace Dashboard.Bff.Clients;

/// <summary>
/// Typed client for reading inventory data from Inventory.Api.
/// </summary>
public sealed class InventoryClient : IInventoryClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public InventoryClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InventorySummaryDto> GetInventorySummaryAsync(int lowStockThreshold, CancellationToken cancellationToken)
    {
        var requestUri = $"/api/inventory/summary?lowStockThreshold={lowStockThreshold}";
        var envelope = await _httpClient.GetFromJsonAsync<ApiEnvelope<InventorySummaryPayload>>(
            requestUri, JsonOptions, cancellationToken);

        var payload = envelope?.Data
            ?? throw new InvalidOperationException($"Inventory.Api returned an empty response for {requestUri}.");

        return new InventorySummaryDto(
            payload.TotalItems,
            payload.TotalQuantity,
            payload.InStock,
            payload.LowStock,
            payload.OutOfStock,
            payload.LowStockThreshold);
    }
}
