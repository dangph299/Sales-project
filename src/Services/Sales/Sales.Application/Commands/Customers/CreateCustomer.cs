using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to create a new customer.
/// </summary>
/// <param name="Name">
/// The customer's name.
/// </param>
/// <param name="Phone">
/// The customer's phone number.
/// </param>
public sealed record CreateCustomer(string Name, string Phone) : IRequest<CustomerDto>;
