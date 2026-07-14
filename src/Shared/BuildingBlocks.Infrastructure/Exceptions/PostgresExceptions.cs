using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Helpers for classifying persistence errors that can affect message idempotency.
/// </summary>
public static class PostgresExceptions
{
    /// <summary>
    /// Determines whether a <see cref="DbUpdateException"/> was caused by a Postgres unique-key
    /// violation (e.g. a duplicate Inbox row inserted by a concurrent/duplicate delivery), as
    /// opposed to some other database failure.
    /// </summary>
    /// <param name="ex">Persistence exception to inspect.</param>
    /// <returns><see langword="true"/> if <paramref name="ex"/>'s inner exception is a <see cref="PostgresException"/> with <see cref="PostgresErrorCodes.UniqueViolation"/>; otherwise <see langword="false"/>.</returns>
    public static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }

    /// <summary>
    /// Determines whether an exception was caused by a Postgres SERIALIZABLE-isolation conflict
    /// (a serialization failure or a detected deadlock), as opposed to some other database failure.
    /// </summary>
    /// <param name="ex">Exception to inspect. Checked directly and one level of unwrapping deep, to
    /// cover both a wrapped and an unwrapped delivery shape.</param>
    /// <returns><see langword="true"/> if <paramref name="ex"/> (or its inner exception) is a <see cref="PostgresException"/> with <see cref="PostgresErrorCodes.SerializationFailure"/> or <see cref="PostgresErrorCodes.DeadlockDetected"/>; otherwise <see langword="false"/>.</returns>
    public static bool IsSerializationConflict(Exception ex)
    {
        // A serialization failure can arrive wrapped in a DbUpdateException (detected mid-
        // SaveChangesAsync) or as a raw, unwrapped PostgresException (detected at CommitAsync,
        // which EF Core never wraps) — check both shapes.
        var postgresException = ex as PostgresException ?? ex.InnerException as PostgresException;
        return postgresException is { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected };
    }
}
