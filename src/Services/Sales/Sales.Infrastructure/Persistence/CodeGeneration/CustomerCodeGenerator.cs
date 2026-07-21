using Sales.Application.Features.Customers.Interfaces;

namespace Sales.Infrastructure;

/// <inheritdoc cref="ICustomerCodeGenerator"/>
public sealed class CustomerCodeGenerator(SequentialCodeGenerator sequentialCodeGenerator) : ICustomerCodeGenerator
{
    /// <inheritdoc />
    public Task<string> NextCodeAsync(CancellationToken cancellationToken)
    {
        return sequentialCodeGenerator.NextCodeAsync(EntityCodeSequence.Customer, cancellationToken);
    }
}
