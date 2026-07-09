using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Generic EF Core implementation of <see cref="IRepository{T}"/>, providing the common CRUD
/// operations shared by every aggregate. Concrete repositories inherit from this and add only
/// their aggregate-specific queries.
/// </summary>
/// <typeparam name="T">
/// The aggregate root type this repository persists.
/// </typeparam>
public class Repository<T>(SalesDbContext db) : IRepository<T> where T : AggregateRoot
{
    /// <summary>
    /// The database context used by this repository and its derived classes.
    /// </summary>
    protected readonly SalesDbContext Db = db;

    /// <inheritdoc/>
    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Db.Set<T>().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<T>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return await Db.Set<T>()
            .Where(x => idList.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task AddAsync(T aggregate, CancellationToken cancellationToken = default) => Db.Set<T>().AddAsync(aggregate, cancellationToken).AsTask();

    /// <inheritdoc/>
    public void Update(T aggregate) => Db.Set<T>().Update(aggregate);

    /// <inheritdoc/>
    public void Delete(T aggregate) => Db.Set<T>().Remove(aggregate);
}
