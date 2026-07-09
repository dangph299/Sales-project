using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to update an existing customer's name and phone number.
/// </summary>
/// <param name="Id">
/// The unique identifier of the customer to update.
/// </param>
/// <param name="Name">
/// The customer's new name.
/// </param>
/// <param name="Phone">
/// The customer's new phone number.
/// </param>
public sealed record UpdateCustomer(Guid Id, string Name, string Phone) : IRequest<CustomerDto>;
