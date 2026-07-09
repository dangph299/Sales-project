namespace Sales.Domain;

/// <summary>
/// A product line requested for an order, materialized (snapshot resolved, no persisted identity yet).
/// Passed to <see cref="Order.Create"/> / <see cref="Order.ReplaceLines"/> to build or replace <see cref="OrderLine"/> entities.
/// </summary>
/// <param name="Product">
/// The resolved product snapshot for this line.
/// </param>
/// <param name="Quantity">
/// The quantity requested.
/// </param>
/// <param name="DiscountPercent">
/// The discount percentage applied to this line.
/// </param>
public sealed record OrderLineItem(ProductSnapshot Product, int Quantity, decimal DiscountPercent);
