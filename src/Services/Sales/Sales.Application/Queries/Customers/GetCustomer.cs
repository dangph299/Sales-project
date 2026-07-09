using MediatR;

namespace Sales.Application;

/// <summary>
/// Query to load a single customer by its identifier.
/// </summary>
/// <param name="Id">
/// The unique identifier of the customer to load.
/// </param>
public sealed record GetCustomer(Guid Id) : IRequest<CustomerDto>;
