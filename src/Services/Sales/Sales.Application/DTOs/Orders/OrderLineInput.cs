namespace Sales.Application;

/// <summary>
/// A requested order line as provided by a caller of <see cref="CreateOrder"/> or
/// <see cref="ReplaceOrderLines"/>, before the product has been resolved.
/// </summary>
/// <param name="ProductId">Product identifier.</param>
/// <param name="Quantity">Requested quantity.</param>
/// <param name="DiscountPercent">Requested discount percentage.</param>
public sealed record OrderLineInput(Guid ProductId, int Quantity, decimal? DiscountPercent);
