using Sales.Application.Features.Orders.Commands;

namespace Sales.Application.Features.Orders.DTOs;

/// <summary>
/// A requested order line as provided by a caller before the product variant has been resolved.
/// </summary>
/// <param name="ProductVariantId">Product variant identifier.</param>
/// <param name="Quantity">Requested quantity.</param>
/// <param name="DiscountPercent">Requested discount percentage.</param>
public sealed record OrderLineInput(Guid ProductVariantId, int Quantity, decimal? DiscountPercent);
