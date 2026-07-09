using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to create a new draft order for a customer.
/// </summary>
/// <param name="CustomerId">
/// The unique identifier of the customer to place the order for.
/// </param>
/// <param name="Lines">
/// The requested product/quantity/discount lines. Must contain at least one line, with no product repeated.
/// </param>
public sealed record CreateOrder(Guid CustomerId, IReadOnlyCollection<OrderLineInput> Lines) : IRequest<OrderDto>;
