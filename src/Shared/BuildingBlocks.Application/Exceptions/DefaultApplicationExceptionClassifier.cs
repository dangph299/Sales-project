using BuildingBlocks.Domain;
using FluentValidation;

namespace BuildingBlocks.Application;

/// <summary>
/// Default classifier for framework-independent application failures.
/// </summary>
public sealed class DefaultApplicationExceptionClassifier : IApplicationExceptionClassifier
{
    /// <inheritdoc/>
    public bool IsExpected(Exception exception) =>
        exception is ValidationException or DomainException || IsOptimisticConcurrencyConflict(exception);

    private static bool IsOptimisticConcurrencyConflict(Exception exception) =>
        exception.GetType().FullName == "Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException";
}
