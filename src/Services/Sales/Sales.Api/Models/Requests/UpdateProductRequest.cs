namespace Sales.Api.Models.Requests;

/// <summary>
/// HTTP request body for <c>PUT /api/products/{id}</c>. Intentionally excludes <c>Id</c>, which
/// comes from the route.
/// </summary>
public sealed class UpdateProductRequest
{
    /// <summary>
    /// Gets the product's new name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the product's new unit price in VND.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Gets whether the product should be active after the update.
    /// </summary>
    public bool IsActive { get; init; }
}
