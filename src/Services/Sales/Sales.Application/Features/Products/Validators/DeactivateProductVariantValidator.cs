using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Products.Commands;

namespace Sales.Application.Features.Products.Validators;

public sealed class DeactivateProductVariantValidator : AbstractValidator<DeactivateProductVariantCommand>
{
    public DeactivateProductVariantValidator()
    {
        RuleFor(x => x.ProductId).ValidAggregateId();
        RuleFor(x => x.VariantId).ValidAggregateId();
    }
}
