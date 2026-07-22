using FluentValidation;
using Sales.Application.Features.Orders.DTOs;

namespace Sales.Application.Features.Orders.Validators;

/// <summary>
/// Validates the customer details supplied with a create-order request: a name and a phone number
/// are both required, so an order can never be created without the customer information the
/// snapshot needs.
/// </summary>
public sealed class CreateOrderCustomerValidator : AbstractValidator<CreateOrderCustomer>
{
    /// <summary>
    /// Configures the validation rules for <see cref="CreateOrderCustomer"/>.
    /// </summary>
    public CreateOrderCustomerValidator()
    {
        RuleFor(x => x.Name).ValidOrderCustomerName();
        RuleFor(x => x.Phone).ValidOrderCustomerPhone();
        RuleFor(x => x.Email).ValidOrderCustomerEmail();
        RuleFor(x => x.Address).ValidOrderCustomerAddress();
    }
}
