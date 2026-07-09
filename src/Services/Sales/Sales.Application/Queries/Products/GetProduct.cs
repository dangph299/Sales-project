using MediatR;

namespace Sales.Application;

/// <summary>
/// Query to load a single product by its identifier. Checks the product cache before falling back
/// to the database.
/// </summary>
/// <param name="Id">
/// The unique identifier of the product to load.
/// </param>
public sealed record GetProduct(Guid Id) : IRequest<ProductDto>;
