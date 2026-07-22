using Sales.Application.Features.Customers.DTOs;

namespace Sales.Application.Features.Customers.Queries;

/// <summary>
/// Query behind the order form's phone autocomplete: finds live customers whose phone number starts
/// with what the user has typed so far.
/// </summary>
/// <param name="Phone">Phone fragment, in any format. Normalized server-side.</param>
/// <param name="Limit">Maximum number of suggestions. Clamped to a small ceiling.</param>
public sealed record LookupCustomersByPhone(string? Phone, int Limit = 10) : IQuery<IReadOnlyCollection<CustomerLookupDto>>;
