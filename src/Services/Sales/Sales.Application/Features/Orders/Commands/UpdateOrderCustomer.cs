using Sales.Application.Features.Orders.DTOs;

namespace Sales.Application.Features.Orders.Commands;

/// <summary>
/// Command to replace the customer details recorded on a draft order, guarded by an optimistic
/// concurrency check.
/// </summary>
/// <remarks>
/// Deliberately separate from <see cref="CreateOrder"/> rather than a shared "save order" path.
/// Creating an order resolves or creates a customer; editing one only rewrites the order's own
/// snapshot and must never reach the customer table at all.
/// </remarks>
/// <param name="Id">Order to edit.</param>
/// <param name="ExpectedVersion">Order's expected version, used to detect concurrent modifications.</param>
/// <param name="Name">Customer's name to record on the order.</param>
/// <param name="Phone">Customer's phone number to record on the order, in any format containing 9 to 15 digits.</param>
/// <param name="Email">Customer's email to record on the order, or <see langword="null"/>.</param>
/// <param name="Address">Customer's address to record on the order, or <see langword="null"/>.</param>
public sealed record UpdateOrderCustomer(
    Guid Id,
    long ExpectedVersion,
    string Name,
    string Phone,
    string? Email,
    string? Address) : ICommand<OrderDto>;
