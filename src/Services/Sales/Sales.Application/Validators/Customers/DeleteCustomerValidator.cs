using FluentValidation;

namespace Sales.Application;

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
