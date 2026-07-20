using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder,
    string Status) : ICommand<CategoryDto>;
