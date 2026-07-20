using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Products.Commands;

namespace Sales.Application.Features.Products.Validators;

/// <summary>
/// Validates <see cref="DeleteProductCommand"/>.
/// </summary>
public sealed class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
{
    /// <summary>
    /// Configures the validation rules.
    /// </summary>
    public DeleteProductValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
    }
}
