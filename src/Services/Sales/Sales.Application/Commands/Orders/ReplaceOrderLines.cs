using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to replace a draft order's lines with a new set, guarded by an optimistic concurrency check.
/// </summary>
/// <param name="Id">Order to edit.</param>
/// <param name="ExpectedVersion">Order's expected version, used to detect concurrent modifications.</param>
/// <param name="Lines">New requested product/quantity/discount lines. Must contain at least one line, with no product repeated.</param>
public sealed record ReplaceOrderLines(Guid Id, long ExpectedVersion, IReadOnlyCollection<OrderLineInput> Lines) : ICommand<OrderDto>;
