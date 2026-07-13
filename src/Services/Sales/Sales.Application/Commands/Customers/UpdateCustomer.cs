using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to update an existing customer's name and phone number.
/// </summary>
/// <param name="Id">Customer identifier.</param>
/// <param name="Name">Customer's new name.</param>
/// <param name="Phone">Customer's new phone number.</param>
public sealed record UpdateCustomer(Guid Id, string Name, string Phone) : ICommand<CustomerDto>;
