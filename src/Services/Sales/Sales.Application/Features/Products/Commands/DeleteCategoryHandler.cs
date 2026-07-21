using MediatR;
using Sales.Application.Common.Exceptions;
using Sales.Application.Common.Interfaces;
using Sales.Domain;

namespace Sales.Application.Features.Products.Commands;

public sealed class DeleteCategoryHandler(
    IRepository<Category> categoryRepository,
    IUnitOfWork unitOfWork,
    IExecutionContext executionContext)
    : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Category), request.Id);

        category.Delete(executionContext.Actor);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
