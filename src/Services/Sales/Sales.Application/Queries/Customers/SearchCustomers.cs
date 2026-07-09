using MediatR;

namespace Sales.Application;

/// <summary>
/// Query to search customers by name and/or phone number.
/// </summary>
/// <param name="Name">
/// An optional substring to match against the customer's name.
/// </param>
/// <param name="Phone">
/// An optional value to match against the customer's phone number.
/// </param>
/// <param name="PhoneMatch">
/// How <paramref name="Phone"/> should be matched (prefix or suffix). Defaults to prefix.
/// </param>
/// <param name="Page">
/// The 1-based page number to return. Defaults to 1.
/// </param>
/// <param name="PageSize">
/// The maximum number of items per page. Defaults to 20.
/// </param>
public sealed record SearchCustomers(string? Name, string? Phone, PhoneMatch PhoneMatch = PhoneMatch.Prefix, int Page = 1, int PageSize = 20) : IRequest<PagedResult<CustomerDto>>;
