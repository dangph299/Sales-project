using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to undo the confirmation of an order, guarded by an optimistic concurrency check.
/// </summary>
/// <param name="Id">Order to undo confirmation for.</param>
/// <param name="ExpectedVersion">Order's expected version, used to detect concurrent modifications.</param>
public sealed record UndoConfirmOrder(Guid Id, long ExpectedVersion) : ICommand<OrderDto>;
