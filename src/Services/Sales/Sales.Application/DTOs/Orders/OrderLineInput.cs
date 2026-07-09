namespace Sales.Application;

/// <summary>
/// A requested order line as provided by a caller of <see cref="CreateOrder"/> or
/// <see cref="ReplaceOrderLines"/>, before the product has been resolved.
/// </summary>
/// <param name="ProductId">
/// The unique identifier of the requested product.
/// </param>
/// <param name="Quantity">
/// The requested quantity.
/// </param>
/// <param name="DiscountPercent">
/// The requested discount percentage.
/// </param>
public sealed record OrderLineInput(Guid ProductId, int Quantity, decimal DiscountPercent);
