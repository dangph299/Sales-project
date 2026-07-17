using Sales.Application.Features.Orders.DTOs;

namespace Sales.Application.Features.Orders.Commands;

/// <summary>
/// Command to cancel an order, guarded by an optimistic concurrency check.
/// </summary>
/// <param name="Id">Order to cancel.</param>
/// <param name="ExpectedVersion">Order's expected version, used to detect concurrent modifications.</param>
public sealed record CancelOrder(Guid Id, long ExpectedVersion) : ICommand<OrderDto>;
