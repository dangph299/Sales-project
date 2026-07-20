using MediatR;
using Sales.Application.Features.Products.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

public sealed class CreateCategoryHandler(
    IRepository<Category> categoryRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        await CategoryCommandSupport.EnsureParentIsValidAsync(
            categoryRepository,
            Guid.NewGuid(),
            request.ParentCategoryId,
            cancellationToken);

        var category = Category.Create(
            request.CategoryCode,
            request.Name,
            request.Description,
            request.ParentCategoryId,
            request.SortOrder);

        await categoryRepository.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CategoryCommandSupport.ToCategoryDto(category);
    }
}
