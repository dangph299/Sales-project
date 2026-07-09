namespace Inventory.Api.Models.Requests;

/// <summary>
/// HTTP request body for the stock adjustment endpoint.
/// </summary>
/// <param name="Sku">
/// The product's SKU, used if a new inventory item needs to be created.
/// </param>
/// <param name="QuantityDelta">
/// The signed quantity to add to the product's available stock.
/// </param>
public sealed record AdjustStockRequest(string Sku, int QuantityDelta);
