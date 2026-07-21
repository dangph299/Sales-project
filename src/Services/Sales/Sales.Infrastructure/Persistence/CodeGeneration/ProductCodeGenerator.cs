using Sales.Application.Features.Products.Interfaces;

namespace Sales.Infrastructure;

/// <inheritdoc cref="IProductCodeGenerator"/>
public sealed class ProductCodeGenerator(SequentialCodeGenerator sequentialCodeGenerator) : IProductCodeGenerator
{
    /// <inheritdoc />
    public Task<string> NextCodeAsync(CancellationToken cancellationToken)
    {
        return sequentialCodeGenerator.NextCodeAsync(EntityCodeSequence.Product, cancellationToken);
    }
}
