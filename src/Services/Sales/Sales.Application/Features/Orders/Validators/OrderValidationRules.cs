using FluentValidation;
using Sales.Application.Features.Orders.DTOs;

namespace Sales.Application.Features.Orders.Validators;

internal static class OrderValidationRules
{
    /// <summary>
    /// Requires that no product identifier occurs more than once across the rule's order lines.
    /// </summary>
    public static IRuleBuilderOptions<T, IReadOnlyCollection<OrderLineInput>> HaveUniqueProducts<T>(
        this IRuleBuilder<T, IReadOnlyCollection<OrderLineInput>> rule) =>
        rule.Must(lines => lines.Count == 0 || lines.Select(x => x.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("A product can occur only once in an order.");
}
