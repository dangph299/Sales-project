using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Products.Commands;

namespace Sales.Application.Features.Products.Validators;

/// <summary>
/// Validates <see cref="UpdateProductCommand"/>.
/// </summary>
public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    /// <summary>
    /// Configures the validation rules for <see cref="UpdateProductCommand"/>.
    /// </summary>
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.CategoryId).ValidAggregateId();
        RuleFor(x => x.Status).NotEmpty().MaximumLength(32);
    }
}
