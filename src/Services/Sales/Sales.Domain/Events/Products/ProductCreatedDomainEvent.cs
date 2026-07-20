namespace Sales.Domain;

/// <summary>
/// Raised when a new <see cref="Product"/> aggregate is created.
/// </summary>
/// <param name="ProductId">Product that was created.</param>
/// <param name="ProductCode">Product's code at creation time.</param>
/// <param name="Name">Product's name at creation time.</param>
/// <param name="CategoryId">Product's category at creation time.</param>
public sealed record ProductCreatedDomainEvent(Guid ProductId, string ProductCode, string Name, Guid CategoryId) : DomainEvent;
