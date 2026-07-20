using Sales.Application.Features.Customers.DTOs;

namespace Sales.Application.Features.Customers.Commands;

public sealed record UpdateCustomerStatusCommand(Guid Id, string Status) : ICommand<CustomerDto>;
