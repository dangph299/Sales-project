using FluentValidation;
using Sales.Application.Features.Orders.Queries;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Validators;

/// <summary>
/// Validates <see cref="SearchOrders"/>.
/// </summary>
/// <remarks>
/// The phone rule matters: a term the caller typed that holds no digit at all cannot be turned into
/// a phone search, and quietly dropping the filter would answer with every order in the system —
/// the opposite of what was asked for. Saying so is the only honest response.
/// </remarks>
public sealed class SearchOrdersValidator : AbstractValidator<SearchOrders>
{
    /// <summary>
    /// Configures the validation rules for <see cref="SearchOrders"/>.
    /// </summary>
    public SearchOrdersValidator()
    {
        RuleFor(x => x.CustomerPhone)
            .Must(HaveAtLeastOneDigitWhenSupplied)
            .WithMessage("Customer phone search must contain at least one digit.");
        RuleFor(x => x.CustomerPhoneMatchMode).IsInEnum();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0);
    }

    private static bool HaveAtLeastOneDigitWhenSupplied(string? customerPhoneSearchTerm)
    {
        if (string.IsNullOrWhiteSpace(customerPhoneSearchTerm))
        {
            return true;
        }

        return CustomerPhoneNormalizer.NormalizeSearchTerm(customerPhoneSearchTerm).Length > 0;
    }
}
