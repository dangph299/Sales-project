using FluentValidation;
using Sales.Application.Features.Customers.Commands;

namespace Sales.Application.Features.Customers.Validators;

/// <summary>
/// Validates <see cref="CreateCustomer"/>: name and phone must be present and well-formed.
/// </summary>
public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomer>
{
    /// <summary>
    /// Configures the validation rules for <see cref="CreateCustomer"/>.
    /// </summary>
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name).ValidCustomerName();
        RuleFor(x => x.Phone).ValidPhone();
    }
}
