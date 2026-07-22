using FluentValidation;
using Sales.Application.Common.Extensions;
using Sales.Application.Features.Orders.Commands;

namespace Sales.Application.Features.Orders.Validators;

/// <summary>
/// Validates <see cref="UpdateOrderCustomer"/>: identifier and expected version must be present, and
/// the customer name and phone number must both be supplied, matching what a create requires.
/// </summary>
public sealed class UpdateOrderCustomerValidator : AbstractValidator<UpdateOrderCustomer>
{
    /// <summary>
    /// Configures the validation rules for <see cref="UpdateOrderCustomer"/>.
    /// </summary>
    public UpdateOrderCustomerValidator()
    {
        RuleFor(x => x.Id).ValidAggregateId();
        RuleFor(x => x.ExpectedVersion).ValidExpectedVersion();
        RuleFor(x => x.Name).ValidOrderCustomerName();
        RuleFor(x => x.Phone).ValidOrderCustomerPhone();
        RuleFor(x => x.Email).ValidOrderCustomerEmail();
        RuleFor(x => x.Address).ValidOrderCustomerAddress();
    }
}
