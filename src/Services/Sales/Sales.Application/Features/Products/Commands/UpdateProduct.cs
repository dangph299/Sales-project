using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Command to update an existing product's name, price, and active flag.
/// </summary>
/// <param name="Id">Product identifier.</param>
/// <param name="Name">Product's new name.</param>
/// <param name="Price">Product's new unit price in VND.</param>
/// <param name="IsActive">Whether the product should be active after the update.</param>
public sealed record UpdateProduct(Guid Id, string Name, decimal Price, bool IsActive) : ICommand<ProductDto>;
