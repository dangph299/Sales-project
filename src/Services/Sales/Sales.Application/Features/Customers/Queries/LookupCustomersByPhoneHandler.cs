using MediatR;
using Sales.Application.Features.Customers.DTOs;
using Sales.Application.Features.Customers.Interfaces;

namespace Sales.Application.Features.Customers.Queries;

/// <summary>
/// Handles <see cref="LookupCustomersByPhone"/> by delegating to the customer read service.
/// </summary>
public sealed class LookupCustomersByPhoneHandler(ICustomerReadService readService)
    : IRequestHandler<LookupCustomersByPhone, IReadOnlyCollection<CustomerLookupDto>>
{
    /// <summary>Smallest number of suggestions a caller can ask for.</summary>
    private const int MinimumLimit = 1;

    /// <summary>
    /// Ceiling on suggestions. A dropdown nobody scrolls does not need more, and the cap keeps a
    /// per-keystroke query cheap however large the customer table grows.
    /// </summary>
    private const int MaximumLimit = 20;

    /// <summary>
    /// Finds customers whose phone number starts with the search term.
    /// </summary>
    /// <param name="request">Query describing the phone fragment and the requested result count.</param>
    /// <returns>Matching customers, or an empty collection when the term holds no digit.</returns>
    public async Task<IReadOnlyCollection<CustomerLookupDto>> Handle(LookupCustomersByPhone request, CancellationToken ct)
    {
        return await readService.LookupByPhonePrefixAsync(
            request.Phone,
            Math.Clamp(request.Limit, MinimumLimit, MaximumLimit),
            ct);
    }
}
