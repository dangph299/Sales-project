namespace Sales.Application;

/// <summary>
/// Read model for a single order line.
/// </summary>
/// <param name="ProductId">
/// The unique identifier of the product this line refers to.
/// </param>
/// <param name="Sku">
/// The product's SKU as it was when the line was created or last replaced.
/// </param>
/// <param name="ProductName">
/// The product's name as it was when the line was created or last replaced.
/// </param>
/// <param name="Quantity">
/// The quantity requested.
/// </param>
/// <param name="UnitPrice">
/// The product's unit price as it was when the line was created or last replaced.
/// </param>
/// <param name="DiscountPercent">
/// The discount percentage (0-100) applied to this line.
/// </param>
/// <param name="LineTotal">
/// The total monetary amount for this line, after discount.
/// </param>
public sealed record OrderLineDto(Guid ProductId, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal DiscountPercent, decimal LineTotal);
