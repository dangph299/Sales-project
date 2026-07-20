namespace Sales.Api.Models.Requests;

/// <summary>
/// HTTP request body for product variant changes.
/// </summary>
public sealed class ProductVariantRequestDto
{
    /// <summary>
    /// Gets the color identifier.
    /// </summary>
    public Guid ColorId { get; init; }

    /// <summary>
    /// Gets the size identifier.
    /// </summary>
    public Guid SizeId { get; init; }

    /// <summary>
    /// Gets the variant price.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Gets the variant status.
    /// </summary>
    public string Status { get; init; } = "Draft";
}
