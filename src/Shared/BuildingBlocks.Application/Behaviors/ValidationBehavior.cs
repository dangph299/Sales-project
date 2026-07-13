using FluentValidation;
using MediatR;

namespace BuildingBlocks.Application;

/// <summary>
/// Runs all registered FluentValidation validators for the request before invoking the next handler.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(x => x.ValidateAsync(context, cancellationToken))))
            .SelectMany(x => x.Errors)
            .Where(x => x is not null)
            .ToArray();

        if (failures.Length > 0) throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}
