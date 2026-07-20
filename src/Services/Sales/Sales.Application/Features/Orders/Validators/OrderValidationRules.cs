using FluentValidation;
using Sales.Application.Features.Orders.DTOs;

namespace Sales.Application.Features.Orders.Validators;

internal static class OrderValidationRules
{
    /// <summary>
    /// Requires that no product variant identifier occurs more than once across the rule's order lines.
    /// </summary>
    public static IRuleBuilderOptions<T, IReadOnlyCollection<OrderLineInput>> HaveUniqueProducts<T>(
        this IRuleBuilder<T, IReadOnlyCollection<OrderLineInput>> rule)
    {
        return rule.Must(lines => lines.Count == 0 || lines.Select(x => x.ProductVariantId).Distinct().Count() == lines.Count)
            .WithMessage("A product variant can occur only once in an order.");
    }
}
