namespace Sales.Api.Models.Requests;

/// <summary>
/// HTTP request body for <c>PUT /api/products/{id}</c>.
/// </summary>
public sealed class UpdateProductRequestDto
{
    /// <summary>
    /// Gets the product's new name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the product description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the category identifier.
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// Gets the product status.
    /// </summary>
    public string Status { get; init; } = "Draft";
}
