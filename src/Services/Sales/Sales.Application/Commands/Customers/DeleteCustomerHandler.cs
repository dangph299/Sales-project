using MediatR;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="DeleteCustomer"/> by soft-deleting the customer.
/// </summary>
public sealed class DeleteCustomerHandler(IRepository<Customer> repository, IUnitOfWork unitOfWork, IExecutionContext context)
    : IRequestHandler<DeleteCustomer>
{
    /// <inheritdoc/>
    public async Task Handle(DeleteCustomer request, CancellationToken ct)
    {
        var customer = await repository.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException(nameof(Customer), request.Id);
        customer.Delete(context.Actor);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
