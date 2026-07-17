using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Customers.Commands;

namespace Sales.Application.Features.Customers.Validators;

/// <summary>
/// Validates <see cref="UpdateCustomer"/>: identifier must be present, and name/phone must be
/// present and well-formed.
/// </summary>
public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomer>
{
    /// <summary>
    /// Configures the validation rules for <see cref="UpdateCustomer"/>.
    /// </summary>
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
        RuleFor(x => x.Name).ValidCustomerName();
        RuleFor(x => x.Phone).ValidPhone();
    }
}
