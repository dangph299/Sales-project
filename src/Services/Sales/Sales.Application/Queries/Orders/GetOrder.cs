using MediatR;

namespace Sales.Application;

/// <summary>
/// Query to load a single order by its identifier.
/// </summary>
/// <param name="Id">
/// The unique identifier of the order to load.
/// </param>
public sealed record GetOrder(Guid Id) : IRequest<OrderDto>;
