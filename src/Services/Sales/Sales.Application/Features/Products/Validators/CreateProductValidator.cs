using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Products.Commands;

namespace Sales.Application.Features.Products.Validators;

/// <summary>
/// Validates <see cref="CreateProductCommand"/>.
/// </summary>
public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>
    /// Configures the validation rules for <see cref="CreateProductCommand"/>.
    /// </summary>
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.CategoryId).ValidAggregateId();
        RuleForEach(x => x.Variants).ChildRules(variant =>
        {
            variant.RuleFor(x => x.ColorId).ValidAggregateId();
            variant.RuleFor(x => x.SizeId).ValidAggregateId();
            variant.RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
            variant.RuleFor(x => x.Status).NotEmpty().MaximumLength(32);
        });
    }
}
