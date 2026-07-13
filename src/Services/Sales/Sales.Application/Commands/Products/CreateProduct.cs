using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to create a new catalog product.
/// </summary>
/// <param name="Sku">Product's SKU. Must be unique across the catalog.</param>
/// <param name="Name">Product's name.</param>
/// <param name="Price">Product's unit price in VND.</param>
public sealed record CreateProduct(string Sku, string Name, decimal Price) : IRequest<ProductDto>;
