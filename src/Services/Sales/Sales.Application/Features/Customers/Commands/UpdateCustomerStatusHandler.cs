using MapsterMapper;
using MediatR;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Customers.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Customers.Commands;

public sealed class UpdateCustomerStatusHandler(
    IRepository<Customer> customerRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateCustomerStatusCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(UpdateCustomerStatusCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.Id, cancellationToken) ??
            throw new NotFoundException(nameof(Customer), request.Id);

        ApplyStatus(customer, ParseCustomerStatus(request.Status));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<CustomerDto>(customer);
    }

    private static ECustomerStatus ParseCustomerStatus(string status)
    {
        if (Enum.TryParse<ECustomerStatus>(status, ignoreCase: true, out var customerStatus))
        {
            return customerStatus;
        }

        throw new DomainException("Customer status is invalid.");
    }

    private static void ApplyStatus(Customer customer, ECustomerStatus customerStatus)
    {
        if (customer.Status == customerStatus) return;
        if (customerStatus == ECustomerStatus.Suspended)
        {
            customer.Suspend();
            return;
        }

        if (customerStatus == ECustomerStatus.Blocked)
        {
            customer.Block();
            return;
        }

        if (customerStatus == ECustomerStatus.Normal)
        {
            customer.Reactivate();
            return;
        }

        throw new DomainException("Customer status transition is invalid.");
    }
}
