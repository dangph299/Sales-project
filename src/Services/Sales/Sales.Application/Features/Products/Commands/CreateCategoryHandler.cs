using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Handles <see cref="CreateCategoryCommand"/>, allocating the category code from the backend.
/// </summary>
public sealed class CreateCategoryHandler(
    IRepository<Category> categoryRepository,
    ICategoryCodeGenerator categoryCodeGenerator,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateCategoryCommand, CategoryDto>
{
    /// <inheritdoc/>
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        await CategoryCommandSupport.EnsureParentIsValidAsync(
            categoryRepository,
            Guid.NewGuid(),
            request.ParentCategoryId,
            cancellationToken);

        // Allocated after the parent check so a rejected request does not consume a number.
        var categoryCode = await categoryCodeGenerator.NextCodeAsync(cancellationToken);
        var category = Category.Create(
            categoryCode,
            request.Name,
            request.Description,
            request.ParentCategoryId,
            request.SortOrder);

        await categoryRepository.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CategoryCommandSupport.ToCategoryDto(category);
    }
}
