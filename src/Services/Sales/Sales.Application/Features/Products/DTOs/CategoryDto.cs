namespace Sales.Application.Features.Products.DTOs;

public sealed record CategoryDto(
    Guid Id,
    string CategoryCode,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder,
    string Status,
    long Version);
