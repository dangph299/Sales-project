using BuildingBlocks.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Translates EF Core/PostgreSQL persistence exceptions into shared error codes.
/// </summary>
public sealed class PostgresPersistenceExceptionClassifier : IPersistenceExceptionClassifier
{
    public PersistenceExceptionClassification? Classify(Exception exception)
    {
        return exception switch
        {
            DbUpdateConcurrencyException => new(ErrorCodes.ConcurrencyConflict, false),
            DbUpdateException ex when PostgresExceptions.IsUniqueViolation(ex) => new(ErrorCodes.UniqueViolation, false),
            _ when PostgresExceptions.IsSerializationConflict(exception) => new(ErrorCodes.ConcurrencyConflict, true),
            _ => null
        };
    }
}
