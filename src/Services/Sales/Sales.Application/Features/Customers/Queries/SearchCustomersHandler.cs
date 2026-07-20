using MediatR;
using Sales.Application.Features.Customers.DTOs;
using Sales.Application.Features.Customers.Interfaces;

namespace Sales.Application.Features.Customers.Queries;

/// <summary>
/// Handles <see cref="SearchCustomers"/> by delegating to the customer read service.
/// </summary>
public sealed class SearchCustomersHandler(ICustomerReadService readService) : IRequestHandler<SearchCustomers, PagedResult<CustomerDto>>
{
    /// <summary>
    /// Searches customers matching the given criteria.
    /// </summary>
    /// <param name="request">Query describing the search criteria and paging.</param>
    /// <returns>A page of matching customers.</returns>
    public async Task<PagedResult<CustomerDto>> Handle(SearchCustomers request, CancellationToken ct)
    {
        return await readService.SearchAsync(request.Name, request.Phone, request.PhoneMatch, request.Page, request.PageSize, ct);
    }
}
