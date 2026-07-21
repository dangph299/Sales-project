using Sales.Application.Features.Products.DTOs;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Command to create a category. The category code is allocated by the backend, so it is not part
/// of the request.
/// </summary>
public sealed record CreateCategoryCommand(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int SortOrder) : ICommand<CategoryDto>;
