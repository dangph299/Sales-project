using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to update an existing product's name, price, and active flag.
/// </summary>
/// <param name="Id">
/// The unique identifier of the product to update.
/// </param>
/// <param name="Name">
/// The product's new name.
/// </param>
/// <param name="Price">
/// The product's new unit price in VND.
/// </param>
/// <param name="IsActive">
/// Whether the product should be active after the update.
/// </param>
public sealed record UpdateProduct(Guid Id, string Name, decimal Price, bool IsActive) : IRequest<ProductDto>;
