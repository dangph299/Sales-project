using MapsterMapper;
using MediatR;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Customers.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Customers.Commands;

/// <summary>
/// Handles <see cref="UpdateCustomer"/> by loading and updating an existing <see cref="Customer"/> aggregate.
/// </summary>
public sealed class UpdateCustomerHandler(
    IRepository<Customer> customerRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateCustomer, CustomerDto>
{
    /// <summary>
    /// Loads the customer, applies the update, and commits the unit of work.
    /// </summary>
    /// <param name="request">Command describing the customer to update and its new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated customer, mapped to a <see cref="CustomerDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown when no customer exists with the given identifier.</exception>
    /// <exception cref="Sales.Domain.DomainException">Thrown when the provided name or phone number is invalid.</exception>
    public async Task<CustomerDto> Handle(UpdateCustomer request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Customer), request.Id);
        customer.Update(request.Name, request.Phone);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<CustomerDto>(customer);
    }
}
