using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Customers.Commands;

namespace Sales.Application.Features.Customers.Validators;

/// <summary>
/// Validates <see cref="DeleteCustomer"/>.
/// </summary>
public sealed class DeleteCustomerValidator : AbstractValidator<DeleteCustomer>
{
    /// <summary>
    /// Configures the validation rules.
    /// </summary>
    public DeleteCustomerValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
    }
}
