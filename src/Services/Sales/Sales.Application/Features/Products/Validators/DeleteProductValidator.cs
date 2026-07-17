using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Products.Commands;

namespace Sales.Application.Features.Products.Validators;

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
