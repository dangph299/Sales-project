using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Features.Customers.DTOs;
using Sales.Application.Features.Customers.Interfaces;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Read-side customer lookup service for query handlers.
/// </summary>
public sealed class CustomerReadService(SalesDbContext db, IMapper mapper) : ICustomerReadService
{
    /// <inheritdoc/>
    public async Task<CustomerDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var activeCustomer = new ActiveCustomerSpecification();
        var customer = await db.Customers.AsNoTracking()
            .Where(activeCustomer.ToExpression())
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return customer is null ? null : mapper.Map<CustomerDto>(customer);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<CustomerDto>> SearchAsync(string? name, string? phone, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var activeCustomer = new ActiveCustomerSpecification();
        var query = db.Customers.AsNoTracking().Where(activeCustomer.ToExpression());
        if (!string.IsNullOrWhiteSpace(name))
        {
            // The name is a literal "contains" fragment: escape its LIKE metacharacters so a typed
            // "%" or "_" finds those characters instead of acting as a wildcard.
            var escapedCustomerNameSearchTerm = LikePatternEscaper.Escape(name.Trim());
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, $"%{escapedCustomerNameSearchTerm}%", LikePatternEscaper.EscapeCharacter));
        }
        if (!string.IsNullOrWhiteSpace(phone))
        {
            // A phone keyword matches customers whose number starts with it (NormalizedPhone prefix)
            // or ends with it (ReversedPhone prefix), so both indexed prefix scans stay usable.
            var normalizedCustomerPhoneSearchTerm = CustomerPhoneNormalizer.NormalizeSearchTerm(phone);
            if (normalizedCustomerPhoneSearchTerm.Length == 0)
            {
                // A phone term that holds no digit (e.g. "abc") is a filter that nothing can match,
                // not the absence of a filter. Building the query anyway would leave both LIKE
                // patterns as a bare "%" and return the whole active customer table, so short-circuit
                // to an empty page instead, mirroring LookupByPhonePrefixAsync.
                return new([], page, pageSize, 0);
            }

            var reversedCustomerPhoneSearchTerm = CustomerPhoneNormalizer.Reverse(normalizedCustomerPhoneSearchTerm);
            query = query.Where(x =>
                EF.Functions.Like(x.NormalizedPhone, normalizedCustomerPhoneSearchTerm + "%")
                || EF.Functions.Like(x.ReversedPhone, reversedCustomerPhoneSearchTerm + "%"));
        }
        var total = await query.LongCountAsync(ct);
        var customers = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(mapper.Map<CustomerDto[]>(customers), page, pageSize, total);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<CustomerLookupDto>> LookupByPhonePrefixAsync(
        string? customerPhoneSearchTerm,
        int limit,
        CancellationToken ct = default)
    {
        var normalizedCustomerPhoneSearchTerm = CustomerPhoneNormalizer.NormalizeSearchTerm(customerPhoneSearchTerm);
        if (normalizedCustomerPhoneSearchTerm.Length == 0)
        {
            return [];
        }

        // Projected straight into the DTO so the query reads only the five columns the dropdown
        // shows, instead of materializing whole customer aggregates on every keystroke. The
        // active-customer filter keeps soft-deleted rows out and keeps the partial phone index
        // eligible.
        var activeCustomer = new ActiveCustomerSpecification();
        return await db.Customers.AsNoTracking()
            .Where(activeCustomer.ToExpression())
            .Where(x => EF.Functions.Like(x.NormalizedPhone, normalizedCustomerPhoneSearchTerm + "%"))
            .OrderBy(x => x.NormalizedPhone)
            .Take(limit)
            .Select(x => new CustomerLookupDto(
                x.Id,
                x.Phone,
                x.Name,
                x.Email,
                x.Address))
            .ToListAsync(ct);
    }
}
