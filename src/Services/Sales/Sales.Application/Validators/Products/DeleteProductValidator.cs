using FluentValidation;

namespace Sales.Application;

/// <summary>
/// Validates <see cref="DeleteProduct"/>.
/// </summary>
public sealed class DeleteProductValidator : AbstractValidator<DeleteProduct>
{
    /// <summary>
    /// Configures the validation rules.
    /// </summary>
    public DeleteProductValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
    }
}
