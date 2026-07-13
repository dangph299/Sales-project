using MediatR;

namespace Sales.Application;

/// <summary>
/// Command to soft-delete an existing customer.
/// </summary>
/// <param name="Id">Customer identifier.</param>
public sealed record DeleteCustomer(Guid Id) : IRequest;
