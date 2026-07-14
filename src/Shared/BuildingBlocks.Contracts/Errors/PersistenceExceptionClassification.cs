namespace BuildingBlocks.Contracts;

/// <summary>
/// Provider-neutral classification for persistence failures that can be exposed by API error
/// handling without leaking database or ORM exception types into the API layer.
/// </summary>
/// <param name="Code">Shared public error code.</param>
/// <param name="Retryable">Whether the exact same request can be retried safely.</param>
public sealed record PersistenceExceptionClassification(string Code, bool Retryable);

/// <summary>
/// Classifies provider-specific persistence exceptions into shared public error codes.
/// </summary>
public interface IPersistenceExceptionClassifier
{
    /// <summary>
    /// Returns a provider-neutral classification, or <see langword="null"/> when the exception is
    /// not a known persistence conflict.
    /// </summary>
    PersistenceExceptionClassification? Classify(Exception exception);
}
