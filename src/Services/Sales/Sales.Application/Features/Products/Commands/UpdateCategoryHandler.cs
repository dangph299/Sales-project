using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Products.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

public sealed class UpdateCategoryHandler(
    IRepository<Category> categoryRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Category), request.Id);
        await CategoryCommandSupport.EnsureParentIsValidAsync(categoryRepository, category.Id, request.ParentCategoryId, cancellationToken);

        category.Update(request.Name, request.Description, request.ParentCategoryId, request.SortOrder);
        ApplyStatus(category, CategoryCommandSupport.ParseCategoryStatus(request.Status));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CategoryCommandSupport.ToCategoryDto(category);
    }

    private static void ApplyStatus(Category category, ECategoryStatus categoryStatus)
    {
        if (category.Status == categoryStatus) return;
        if (categoryStatus == ECategoryStatus.Published)
        {
            category.Publish();
            return;
        }

        if (categoryStatus == ECategoryStatus.Archived)
        {
            category.Archive();
            return;
        }

        throw new DomainException("Category status transition is invalid.");
    }
}
