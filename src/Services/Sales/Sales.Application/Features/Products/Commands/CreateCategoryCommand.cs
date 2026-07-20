using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

public sealed record CreateCategoryCommand(
    string CategoryCode,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder) : ICommand<CategoryDto>;
