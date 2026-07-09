using MediatR;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="GetCustomer"/> by delegating to the customer read service.
/// </summary>
public sealed class GetCustomerHandler(ICustomerReadService readService) : IRequestHandler<GetCustomer, CustomerDto>
{
    /// <summary>
    /// Loads the requested customer.
    /// </summary>
    /// <param name="request">
    /// The query identifying the customer to load.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The customer, mapped to a <see cref="CustomerDto"/>.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// Thrown when no customer exists with the given identifier.
    /// </exception>
    public async Task<CustomerDto> Handle(GetCustomer request, CancellationToken ct) =>
        await readService.GetAsync(request.Id, ct) ?? throw new NotFoundException("Customer", request.Id);
}
