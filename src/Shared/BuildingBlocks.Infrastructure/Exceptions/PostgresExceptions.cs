using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Helpers for classifying Postgres errors surfaced through EF Core, shared by the Sales and
/// Inventory Kafka consumer handlers so each doesn't keep its own copy of the same detection logic.
/// </summary>
public static class PostgresExceptions
{
    /// <summary>
    /// Determines whether a <see cref="DbUpdateException"/> was caused by a Postgres unique-key
    /// violation (e.g. a duplicate Inbox row inserted by a concurrent/duplicate delivery), as
    /// opposed to some other database failure.
    /// </summary>
    /// <param name="ex">
    /// The EF Core update exception to inspect.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="ex"/>'s inner exception is a
    /// <see cref="PostgresException"/> with <see cref="PostgresErrorCodes.UniqueViolation"/>;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
