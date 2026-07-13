using Microsoft.EntityFrameworkCore;
using Sales.Application;

namespace Sales.Infrastructure;

/// <summary>
/// Read-side customer lookup service for query handlers.
/// </summary>
public sealed class CustomerReadService(SalesDbContext db) : ICustomerReadService
{
    /// <inheritdoc/>
    public Task<CustomerDto?> GetAsync(Guid id, CancellationToken ct = default) => db.Customers.AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new CustomerDto(x.Id, x.Name, x.Phone, x.Version, x.UpdatedAt, x.IsDelete, x.DeleteByUser, x.DeletedAt))
        .SingleOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<PagedResult<CustomerDto>> SearchAsync(string? name, string? phone, PhoneMatch phoneMatch, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var query = db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(name)) query = query.Where(x => EF.Functions.ILike(x.Name, $"%{name.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var normalized = new string(phone.Where(char.IsDigit).ToArray());
            query = phoneMatch == PhoneMatch.Suffix
                ? query.Where(x => x.ReversedPhone.StartsWith(new string(normalized.Reverse().ToArray())))
                : query.Where(x => x.Phone.StartsWith(normalized));
        }
        var total = await query.LongCountAsync(ct);
        var items = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CustomerDto(x.Id, x.Name, x.Phone, x.Version, x.UpdatedAt, x.IsDelete, x.DeleteByUser, x.DeletedAt))
            .ToListAsync(ct);
        return new(items, page, pageSize, total);
    }
}
