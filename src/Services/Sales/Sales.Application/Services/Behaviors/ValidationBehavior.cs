using FluentValidation;
using MediatR;

namespace Sales.Application;

/// <summary>
/// Runs all registered FluentValidation validators for the request before invoking the next
/// handler in the pipeline, short-circuiting with a <see cref="ValidationException"/> if any fail.
/// </summary>
/// <typeparam name="TRequest">
/// The MediatR request type being validated.
/// </typeparam>
/// <typeparam name="TResponse">
/// The response type returned by the request handler.
/// </typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Validates the request and, if valid, invokes the next handler in the pipeline.
    /// </summary>
    /// <param name="request">
    /// The request to validate.
    /// </param>
    /// <param name="next">
    /// The next delegate in the pipeline.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The response produced by <paramref name="next"/>.
    /// </returns>
    /// <exception cref="ValidationException">
    /// Thrown when one or more registered validators report a failure.
    /// </exception>
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
