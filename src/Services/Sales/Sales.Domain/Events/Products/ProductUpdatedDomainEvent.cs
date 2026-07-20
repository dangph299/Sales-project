namespace Sales.Domain;

/// <summary>
/// Raised when an existing <see cref="Product"/> aggregate's common details are changed.
/// </summary>
/// <param name="ProductId">Product that was updated.</param>
/// <param name="OldName">Product's name before the update.</param>
/// <param name="OldCategoryId">Product's category before the update.</param>
/// <param name="OldStatus">Product's status before the update.</param>
/// <param name="NewName">Product's name after the update.</param>
/// <param name="NewCategoryId">Product's category after the update.</param>
/// <param name="NewStatus">Product's status after the update.</param>
public sealed record ProductUpdatedDomainEvent(
    Guid ProductId,
    string OldName,
    Guid OldCategoryId,
    EProductStatus OldStatus,
    string NewName,
    Guid NewCategoryId,
    EProductStatus NewStatus) : DomainEvent;
