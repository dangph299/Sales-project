using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Products.Commands;

namespace Sales.Application.Features.Products.Validators;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ParentCategoryId)
            .Must(parentCategoryId => !parentCategoryId.HasValue || parentCategoryId.Value != Guid.Empty)
            .WithMessage("Parent category id must not be empty.");
        RuleFor(x => x.Status).NotEmpty().MaximumLength(32);
    }
}
