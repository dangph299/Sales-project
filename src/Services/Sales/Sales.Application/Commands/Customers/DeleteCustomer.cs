using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to soft-delete an existing customer.
/// </summary>
/// <param name="Id">
/// The unique identifier of the customer to delete.
/// </param>
public sealed record DeleteCustomer(Guid Id) : IRequest;
