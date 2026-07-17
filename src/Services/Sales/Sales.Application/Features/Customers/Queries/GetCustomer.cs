using Sales.Application.Features.Customers.DTOs;

namespace Sales.Application.Features.Customers.Queries;

/// <summary>
/// Query to load a single customer by its identifier.
/// </summary>
/// <param name="Id">Customer identifier.</param>
public sealed record GetCustomer(Guid Id) : IQuery<CustomerDto>;
