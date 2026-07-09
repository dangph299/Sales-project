using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to cancel an order, guarded by an optimistic concurrency check.
/// </summary>
/// <param name="Id">
/// The unique identifier of the order to cancel.
/// </param>
/// <param name="ExpectedVersion">
/// The order's expected version, used to detect concurrent modifications.
/// </param>
public sealed record CancelOrder(Guid Id, long ExpectedVersion) : IRequest<OrderDto>;
