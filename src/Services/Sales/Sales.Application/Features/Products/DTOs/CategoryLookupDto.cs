namespace Sales.Application.Features.Products.DTOs;

/// <summary>
/// Category published by the lookup endpoint. Carries the stable <see cref="CategoryCode"/> that
/// clients match on plus the persistence <see cref="Id"/> that write requests must submit.
/// </summary>
public sealed record CategoryLookupDto(
    Guid Id,
    string CategoryCode,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder,
    string Status);
