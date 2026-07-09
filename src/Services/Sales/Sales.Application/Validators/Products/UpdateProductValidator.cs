using FluentValidation;

namespace Sales.Application;

/// <summary>
/// Validates <see cref="UpdateProduct"/>: identifier must be present, name must be present and
/// within length limits, and price must be non-negative.
/// </summary>
public sealed class UpdateProductValidator : AbstractValidator<UpdateProduct>
{
    /// <summary>
    /// Configures the validation rules for <see cref="UpdateProduct"/>.
    /// </summary>
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
