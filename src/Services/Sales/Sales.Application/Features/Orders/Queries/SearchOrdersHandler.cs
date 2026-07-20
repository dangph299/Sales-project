using MediatR;
using Sales.Application.Features.Orders.DTOs;
using Sales.Application.Features.Orders.Interfaces;

namespace Sales.Application.Features.Orders.Queries;

/// <summary>
/// Handles <see cref="SearchOrders"/> by delegating to the order read service.
/// </summary>
public sealed class SearchOrdersHandler(IOrderReadService readService) : IRequestHandler<SearchOrders, PagedResult<OrderDto>>
{
    /// <summary>
    /// Searches orders matching the given criteria.
    /// </summary>
    /// <param name="request">Query describing the search criteria and paging.</param>
    /// <returns>A page of matching orders.</returns>
    public async Task<PagedResult<OrderDto>> Handle(SearchOrders request, CancellationToken ct)
    {
        return await readService.SearchAsync(request.From, request.To, request.Customer, request.Page, request.PageSize, ct);
    }
}
