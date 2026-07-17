using FluentValidation;
using Sales.Application.Features.Products.Commands;

namespace Sales.Application.Features.Products.Validators;

/// <summary>
/// Validates <see cref="CreateProduct"/>: SKU and name must be present and within length limits,
/// and price must be non-negative.
/// </summary>
public sealed class CreateProductValidator : AbstractValidator<CreateProduct>
{
    /// <summary>
    /// Configures the validation rules for <see cref="CreateProduct"/>.
    /// </summary>
    public CreateProductValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
