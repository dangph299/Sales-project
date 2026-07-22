using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Customer persistence adapter adding normalized-phone lookup and the advisory lock that
/// serialises concurrent resolve-or-create attempts for one phone number.
/// </summary>
public sealed class CustomerRepository(SalesDbContext db) : Repository<Customer>(db), ICustomerRepository
{
    /// <inheritdoc/>
    public Task<Customer?> FindByNormalizedPhoneAsync(string normalizedCustomerPhone, CancellationToken cancellationToken = default)
    {
        return Db.Customers.SingleOrDefaultAsync(x => x.NormalizedPhone == normalizedCustomerPhone, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AcquireNormalizedPhoneLockAsync(string normalizedCustomerPhone, CancellationToken cancellationToken = default)
    {
        // Advisory locks are a PostgreSQL feature. The SQLite-backed tests run one connection at a
        // time, so skipping the lock there changes nothing they can observe, and the unique index
        // still rejects a duplicate on any provider.
        if (!Db.Database.IsNpgsql())
        {
            return;
        }

        await Db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({normalizedCustomerPhone}))",
            cancellationToken);
    }
}
