using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Customers.Commands;

namespace Sales.Application.Features.Customers.Validators;

public sealed class UpdateCustomerStatusValidator : AbstractValidator<UpdateCustomerStatusCommand>
{
    public UpdateCustomerStatusValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
        RuleFor(x => x.Status).NotEmpty().MaximumLength(32);
    }
}
