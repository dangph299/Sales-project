using FluentValidation;
using Sales.Application.Features.Orders.Commands;

namespace Sales.Application.Features.Orders.Validators;

/// <summary>
/// Validates expired open order cancellation batches.
/// </summary>
public sealed class CancelExpiredPendingOrdersValidator : AbstractValidator<CancelExpiredPendingOrders>
{
    /// <summary>
    /// Creates validation rules for batch expiration cancellation.
    /// </summary>
    public CancelExpiredPendingOrdersValidator()
    {
        RuleFor(x => x.CurrentUtc).NotEqual(default(DateTimeOffset));
        RuleFor(x => x.ExpirationMinutes).GreaterThan(0);
        RuleFor(x => x.BatchSize).GreaterThan(0);
    }
}
