using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Resolves an EF entity entry to the aggregate-level audit identity it should be grouped under.
/// </summary>
public interface IAuditAggregateResolver
{
    /// <summary>
    /// Resolves the aggregate identity for the changed entity entry.
    /// </summary>
    /// <param name="entityEntry">Changed entity entry.</param>
    /// <returns>Aggregate identity and property prefix for the entry.</returns>
    AuditAggregateIdentity Resolve(EntityEntry entityEntry);
}
