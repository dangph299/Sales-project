using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Command to create a new catalog product.
/// </summary>
/// <param name="Sku">Product's SKU. Must be unique across the catalog.</param>
/// <param name="Name">Product's name.</param>
/// <param name="Price">Product's unit price in VND.</param>
public sealed record CreateProduct(string Sku, string Name, decimal Price) : ICommand<ProductDto>;
