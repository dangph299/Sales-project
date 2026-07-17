using MapsterMapper;
using MediatR;
using Sales.Application.Features.Customers.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Customers.Commands;

/// <summary>
/// Handles <see cref="CreateCustomer"/> by creating and persisting a new <see cref="Customer"/> aggregate.
/// </summary>
public sealed class CreateCustomerHandler(
    IRepository<Customer> customerRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateCustomer, CustomerDto>
{
    /// <summary>
    /// Creates a new customer and commits the unit of work.
    /// </summary>
    /// <param name="request">Command describing the customer to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created customer, mapped to a <see cref="CustomerDto"/>.</returns>
    /// <exception cref="Sales.Domain.DomainException">Thrown when the provided name or phone number is invalid.</exception>
    public async Task<CustomerDto> Handle(CreateCustomer request, CancellationToken cancellationToken)
    {
        var customer = Customer.Create(request.Name, request.Phone);
        await customerRepository.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<CustomerDto>(customer);
    }
}
