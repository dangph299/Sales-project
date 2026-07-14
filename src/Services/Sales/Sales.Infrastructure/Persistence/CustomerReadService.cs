using Microsoft.EntityFrameworkCore;
using Sales.Application;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Read-side customer lookup service for query handlers.
/// </summary>
public sealed class CustomerReadService(SalesDbContext db) : ICustomerReadService
{
    /// <inheritdoc/>
    public async Task<CustomerDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var activeCustomer = new ActiveCustomerSpecification();
        var customer = await db.Customers.AsNoTracking()
            .Where(activeCustomer.ToExpression())
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return customer?.ToDto();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<CustomerDto>> SearchAsync(string? name, string? phone, PhoneMatch phoneMatch, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var activeCustomer = new ActiveCustomerSpecification();
        var query = db.Customers.AsNoTracking().Where(activeCustomer.ToExpression());
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => EF.Functions.ILike(x.Name, $"%{name.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var normalized = new string(phone.Where(char.IsDigit).ToArray());
            query = phoneMatch == PhoneMatch.Suffix
                ? query.Where(x => x.ReversedPhone.StartsWith(new string(normalized.Reverse().ToArray())))
                : query.Where(x => x.Phone.StartsWith(normalized));
        }
        var total = await query.LongCountAsync(ct);
        var customers = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(customers.Select(x => x.ToDto()).ToArray(), page, pageSize, total);
    }
}
