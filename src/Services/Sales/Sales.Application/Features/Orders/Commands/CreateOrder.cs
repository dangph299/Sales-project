using Sales.Application.Features.Orders.DTOs;

namespace Sales.Application.Features.Orders.Commands;

/// <summary>
/// Command to create a new draft order for a customer.
/// </summary>
/// <param name="CustomerId">Customer to place the order for.</param>
/// <param name="Lines">Requested product/quantity/discount lines. Must contain at least one line, with no product repeated.</param>
public sealed record CreateOrder(Guid CustomerId, IReadOnlyCollection<OrderLineInput> Lines) : ICommand<OrderDto>;
