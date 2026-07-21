using Sales.Application.Features.Products.Interfaces;

namespace Sales.Infrastructure;

/// <inheritdoc cref="ICategoryCodeGenerator"/>
public sealed class CategoryCodeGenerator(SequentialCodeGenerator sequentialCodeGenerator) : ICategoryCodeGenerator
{
    /// <inheritdoc />
    public Task<string> NextCodeAsync(CancellationToken cancellationToken)
    {
        return sequentialCodeGenerator.NextCodeAsync(EntityCodeSequence.Category, cancellationToken);
    }
}
