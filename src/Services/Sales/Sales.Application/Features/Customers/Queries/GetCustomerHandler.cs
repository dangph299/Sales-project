using MediatR;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Customers.DTOs;
using Sales.Application.Features.Customers.Interfaces;

namespace Sales.Application.Features.Customers.Queries;

/// <summary>
/// Handles <see cref="GetCustomer"/> by delegating to the customer read service.
/// </summary>
public sealed class GetCustomerHandler(ICustomerReadService readService) : IRequestHandler<GetCustomer, CustomerDto>
{
    /// <summary>
    /// Loads the requested customer.
    /// </summary>
    /// <param name="request">Query identifying the customer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Customer, mapped to a <see cref="CustomerDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown when no customer exists with the given identifier.</exception>
    public async Task<CustomerDto> Handle(GetCustomer request, CancellationToken ct) =>
        await readService.GetAsync(request.Id, ct) ?? throw new NotFoundException("Customer", request.Id);
}
