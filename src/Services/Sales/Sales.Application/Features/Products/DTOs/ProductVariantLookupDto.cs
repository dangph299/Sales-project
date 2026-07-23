namespace Sales.Application.Features.Products.DTOs;

/// <summary>
/// Read model for inventory and order screens that page through product variants directly.
/// </summary>
public sealed record ProductVariantLookupDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string ProductStatus,
    Guid ProductVariantId,
    string Sku,
    ProductColorDto Color,
    ProductSizeDto Size,
    decimal Price,
    string VariantStatus);
