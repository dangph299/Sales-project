namespace Sales.Application;

/// <summary>
/// Read model for a single order line.
/// </summary>
/// <param name="ProductId">Product this line refers to.</param>
/// <param name="Sku">Product's SKU as it was when the line was created or last replaced.</param>
/// <param name="ProductName">Product's name as it was when the line was created or last replaced.</param>
/// <param name="Quantity">Quantity requested.</param>
/// <param name="UnitPrice">Product's unit price as it was when the line was created or last replaced.</param>
/// <param name="DiscountPercent">Discount percentage (0-100) applied to this line.</param>
/// <param name="LineTotal">Total monetary amount for this line, after discount.</param>
public sealed record OrderLineDto(Guid ProductId, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal DiscountPercent, decimal LineTotal);
