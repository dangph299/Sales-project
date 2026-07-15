using MediatR;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="DeleteCustomer"/> by soft-deleting the customer.
/// </summary>
public sealed class DeleteCustomerHandler(
    IRepository<Customer> customerRepository,
    IUnitOfWork unitOfWork,
    IExecutionContext executionContext)
    : IRequestHandler<DeleteCustomer>
{
    /// <inheritdoc/>
    public async Task Handle(DeleteCustomer request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Customer), request.Id);
        customer.Delete(executionContext.Actor);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
