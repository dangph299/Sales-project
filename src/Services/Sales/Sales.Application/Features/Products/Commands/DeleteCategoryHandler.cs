using Sales.Application.Common.Exceptions;
using Sales.Application.Common.Interfaces;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

/// <summary>
/// Soft-deletes a category once nothing still depends on it.
/// </summary>
public sealed class DeleteCategoryHandler(
    IRepository<Category> categoryRepository,
    ICategoryReadService categoryReadService,
    IUnitOfWork unitOfWork,
    IExecutionContext executionContext)
    : ICommandHandler<DeleteCategoryCommand>
{
    /// <summary>
    /// Soft-deletes the requested category.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when no category has the requested identifier.</exception>
    /// <exception cref="DomainException">Thrown when a child category or product still references it.</exception>
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Category), request.Id);

        // Deleting a referenced category would strand its children and products behind a parent that
        // no longer resolves, which the client can only render as a broken tree row.
        if (await categoryReadService.HasDependentsAsync(category.Id, cancellationToken))
        {
            throw new DomainException(
                "Category cannot be deleted while child categories or products still reference it.");
        }

        category.Delete(executionContext.Actor);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
