using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to soft-delete an existing product.
/// </summary>
/// <param name="Id">
/// The unique identifier of the product to delete.
/// </param>
public sealed record DeleteProduct(Guid Id) : IRequest;
