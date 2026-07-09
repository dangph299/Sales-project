namespace Sales.Domain;

/// <summary>
/// Thrown when an operation would violate a business invariant of the domain model.
/// </summary>
/// <param name="message">
/// A human-readable description of the invariant that was violated.
/// </param>
public sealed class DomainException(string message) : Exception(message);
