namespace Sales.Application;

/// <summary>
/// Command to request confirmation of a draft order, guarded by an optimistic concurrency check.
/// </summary>
/// <param name="Id">Order to confirm.</param>
/// <param name="ExpectedVersion">Order's expected version, used to detect concurrent modifications.</param>
public sealed record ConfirmOrder(Guid Id, long ExpectedVersion) : ICommand<OrderDto>;
