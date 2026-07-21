using Sales.Application.Features.Customers.DTOs;

namespace Sales.Application.Features.Customers.Commands;

/// <summary>
/// Command to update an existing customer's contact details.
/// </summary>
/// <param name="Id">Customer identifier.</param>
/// <param name="Name">Customer's new name.</param>
/// <param name="Phone">Customer's new phone number.</param>
/// <param name="Email">Customer's email address.</param>
/// <param name="Address">Customer's address.</param>
public sealed record UpdateCustomer(Guid Id, string Name, string Phone, string? Email = null, string? Address = null) : ICommand<CustomerDto>;
