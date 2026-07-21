using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Products.Commands;

namespace Sales.Application.Features.Products.Validators;

public sealed class DeleteProductVariantValidator : AbstractValidator<DeleteProductVariantCommand>
{
    public DeleteProductVariantValidator()
    {
        RuleFor(x => x.ProductId).ValidAggregateId();
        RuleFor(x => x.VariantId).ValidAggregateId();
    }
}
