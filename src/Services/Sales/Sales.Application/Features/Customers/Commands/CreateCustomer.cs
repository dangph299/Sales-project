using Sales.Application.Features.Customers.DTOs;

namespace Sales.Application.Features.Customers.Commands;

/// <summary>
/// Command to create a new customer.
/// </summary>
/// <param name="Name">Customer's name.</param>
/// <param name="Phone">Customer's phone number.</param>
public sealed record CreateCustomer(string Name, string Phone) : ICommand<CustomerDto>;
