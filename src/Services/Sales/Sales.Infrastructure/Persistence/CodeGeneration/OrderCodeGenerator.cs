using Sales.Application.Features.Orders.Interfaces;

namespace Sales.Infrastructure;

/// <inheritdoc cref="IOrderCodeGenerator"/>
public sealed class OrderCodeGenerator(SequentialCodeGenerator sequentialCodeGenerator) : IOrderCodeGenerator
{
    /// <inheritdoc />
    public Task<string> NextCodeAsync(CancellationToken cancellationToken)
    {
        return sequentialCodeGenerator.NextCodeAsync(EntityCodeSequence.Order, cancellationToken);
    }
}
