using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to request confirmation of a draft order, guarded by an optimistic concurrency check.
/// </summary>
/// <param name="Id">
/// The unique identifier of the order to confirm.
/// </param>
/// <param name="ExpectedVersion">
/// The order's expected version, used to detect concurrent modifications.
/// </param>
public sealed record ConfirmOrder(Guid Id, long ExpectedVersion) : IRequest<OrderDto>;
