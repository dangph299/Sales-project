using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Products.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

internal static class CategoryCommandSupport
{
    public static CategoryDto ToCategoryDto(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.CategoryCode,
            category.Name,
            category.Description,
            category.ParentCategoryId,
            category.SortOrder,
            category.Status.ToString(),
            category.Version,
            category.CreatedAt,
            category.UpdatedAt);
    }

    public static ECategoryStatus ParseCategoryStatus(string status)
    {
        if (Enum.TryParse<ECategoryStatus>(status, ignoreCase: true, out var categoryStatus))
        {
            return categoryStatus;
        }

        throw new DomainException("Category status is invalid.");
    }

    public static async Task EnsureParentIsValidAsync(
        IRepository<Category> categoryRepository,
        Guid categoryId,
        Guid? parentCategoryId,
        CancellationToken cancellationToken)
    {
        if (!parentCategoryId.HasValue)
        {
            return;
        }

        var visitedCategoryIds = new HashSet<Guid> { categoryId };
        var currentParentId = parentCategoryId.Value;
        while (currentParentId != Guid.Empty)
        {
            if (!visitedCategoryIds.Add(currentParentId))
            {
                throw new DomainException("Category parent hierarchy cannot be circular.");
            }

            var parentCategory = await categoryRepository.GetByIdAsync(currentParentId, cancellationToken) ??
                throw new NotFoundException(nameof(Category), currentParentId);
            if (!parentCategory.ParentCategoryId.HasValue)
            {
                return;
            }

            currentParentId = parentCategory.ParentCategoryId.Value;
        }
    }
}
