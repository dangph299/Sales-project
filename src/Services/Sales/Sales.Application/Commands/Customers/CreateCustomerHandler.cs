using MediatR;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="CreateCustomer"/> by creating and persisting a new <see cref="Customer"/> aggregate.
/// </summary>
public sealed class CreateCustomerHandler(IRepository<Customer> repository, IUnitOfWork unitOfWork) : IRequestHandler<CreateCustomer, CustomerDto>
{
    /// <summary>
    /// Creates a new customer and commits the unit of work.
    /// </summary>
    /// <param name="request">
    /// The command describing the customer to create.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The created customer, mapped to a <see cref="CustomerDto"/>.
    /// </returns>
    /// <exception cref="Sales.Domain.DomainException">
    /// Thrown when the provided name or phone number is invalid.
    /// </exception>
    public async Task<CustomerDto> Handle(CreateCustomer request, CancellationToken ct)
    {
        var customer = Customer.Create(request.Name, request.Phone);
        await repository.AddAsync(customer, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return customer.ToDto();
    }
}
