using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Products.Commands;

namespace Sales.Application.Features.Products.Validators;

public sealed class AddProductVariantValidator : AbstractValidator<AddProductVariantCommand>
{
    public AddProductVariantValidator()
    {
        RuleFor(x => x.ProductId).ValidAggregateId();
        RuleFor(x => x.ColorId).ValidAggregateId();
        RuleFor(x => x.SizeId).ValidAggregateId();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(32);
    }
}
