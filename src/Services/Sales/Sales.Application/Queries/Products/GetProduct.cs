using MediatR;

namespace Sales.Application;

/// <summary>
/// Query to load a single product by its identifier. Checks the product cache before falling back
/// to the database.
/// </summary>
/// <param name="Id">Product identifier.</param>
public sealed record GetProduct(Guid Id) : IRequest<ProductDto>;
