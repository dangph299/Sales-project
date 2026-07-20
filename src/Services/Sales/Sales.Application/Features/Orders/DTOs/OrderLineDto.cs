namespace Sales.Application.Features.Orders.DTOs;

/// <summary>
/// Read model for a single order line.
/// </summary>
/// <param name="ProductId">Product this line refers to.</param>
/// <param name="ProductVariantId">Product variant this line refers to.</param>
/// <param name="ProductCode">Product code as it was when the line was created or last replaced.</param>
/// <param name="Sku">Product's SKU as it was when the line was created or last replaced.</param>
/// <param name="ProductName">Product's name as it was when the line was created or last replaced.</param>
/// <param name="ColorCode">Color code as it was when the line was created or last replaced.</param>
/// <param name="ColorName">Color name as it was when the line was created or last replaced.</param>
/// <param name="SizeCode">Size code as it was when the line was created or last replaced.</param>
/// <param name="Quantity">Quantity requested.</param>
/// <param name="UnitPrice">Product's unit price as it was when the line was created or last replaced.</param>
/// <param name="DiscountPercent">Discount percentage (0-100) applied to this line.</param>
/// <param name="LineTotal">Total monetary amount for this line, after discount.</param>
public sealed record OrderLineDto(
    Guid ProductId,
    Guid ProductVariantId,
    string ProductCode,
    string Sku,
    string ProductName,
    string ColorCode,
    string ColorName,
    string SizeCode,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal LineTotal);
