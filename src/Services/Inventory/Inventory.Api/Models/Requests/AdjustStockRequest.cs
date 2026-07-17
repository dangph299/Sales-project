namespace Inventory.Api.Models.Requests;

/// <summary>
/// HTTP request body for the stock adjustment endpoint.
/// </summary>
/// <param name="Sku">Product's SKU, used if a new inventory item needs to be created.</param>
/// <param name="QuantityDelta">Signed quantity to add to the product's available stock.</param>
public sealed record AdjustStockRequest(string Sku, int QuantityDelta);
