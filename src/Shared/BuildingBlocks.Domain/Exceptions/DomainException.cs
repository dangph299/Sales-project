namespace BuildingBlocks.Domain;

/// <summary>
/// Thrown when an operation would violate a domain invariant.
/// </summary>
/// <param name="message">
/// A human-readable description of the invariant that was violated.
/// </param>
public class DomainException(string message) : Exception(message);
