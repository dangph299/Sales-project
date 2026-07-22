using Sales.Application.Features.Orders.DTOs;

namespace Sales.Application.Features.Orders.Commands;

/// <summary>
/// Command to create a new draft order for a customer.
/// </summary>
/// <param name="Customer">Customer details for the order. The backend resolves them to an existing customer by phone number, or creates one; the caller never decides.</param>
/// <param name="Lines">Requested product/quantity/discount lines. Must contain at least one line, with no product repeated.</param>
public sealed record CreateOrder(CreateOrderCustomer Customer, IReadOnlyCollection<OrderLineInput> Lines) : ICommand<OrderDto>;
