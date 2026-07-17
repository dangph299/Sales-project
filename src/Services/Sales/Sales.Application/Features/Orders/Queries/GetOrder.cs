using Sales.Application.Features.Orders.DTOs;

namespace Sales.Application.Features.Orders.Queries;

/// <summary>
/// Query to load a single order by its identifier.
/// </summary>
/// <param name="Id">Order identifier.</param>
public sealed record GetOrder(Guid Id) : IQuery<OrderDto>;
