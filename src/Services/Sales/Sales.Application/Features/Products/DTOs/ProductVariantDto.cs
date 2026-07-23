namespace Sales.Application.Features.Products.DTOs;

public sealed record ProductVariantDto(
    Guid Id,
    string Sku,
    ProductColorDto Color,
    ProductSizeDto Size,
    decimal Price,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
