using Sales.Application.Features.Customers.DTOs;

namespace Sales.Application.Features.Customers.Queries;

/// <summary>
/// Query to search customers by name and/or phone number.
/// </summary>
/// <param name="Name">An optional substring to match against the customer's name.</param>
/// <param name="Phone">An optional value matched against the start or the end of the customer's phone number.</param>
/// <param name="Page">1-based page number. Defaults to 1.</param>
/// <param name="PageSize">Maximum page size. Defaults to 20.</param>
public sealed record SearchCustomers(string? Name, string? Phone, int Page = 1, int PageSize = 20) : IQuery<PagedResult<CustomerDto>>;
