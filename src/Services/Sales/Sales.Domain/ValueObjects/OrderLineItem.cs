namespace Sales.Domain;

/// <summary>
/// A product line requested for an order, materialized (snapshot resolved, no persisted identity yet).
/// Passed to <see cref="Order.Create"/> / <see cref="Order.ReplaceLines"/> to build or replace <see cref="OrderLine"/> entities.
/// </summary>
/// <param name="Product">Resolved product snapshot for this line.</param>
/// <param name="Quantity">Quantity requested.</param>
/// <param name="DiscountPercent">Discount percentage applied to this line.</param>
public sealed record OrderLineItem(ProductSnapshot Product, int Quantity, decimal DiscountPercent);
