namespace Sales.Application.Features.Products.DTOs;

/// <summary>
/// Read model for a product, returned by queries, API responses, and the product cache.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool IsActive,
    long Version,
    DateTimeOffset UpdatedAt,
    bool IsDelete,
    string? DeleteByUser,
    DateTimeOffset? DeletedAt)
{
    public string? ProductCode { get; init; }

    public string? Description { get; init; }

    public Guid? CategoryId { get; init; }

    public string? Status { get; init; }

    public ProductCategoryDto? Category { get; init; }

    public IReadOnlyCollection<ProductVariantDto>? Variants { get; init; }
}
