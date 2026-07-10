using Mapster;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Extension methods mapping Sales aggregates to their read-model DTOs.
/// </summary>
public static class DtoMapping
{
    private static readonly TypeAdapterConfig Config = CreateConfig();

    /// <summary>
    /// Maps a <see cref="Product"/> aggregate to a <see cref="ProductDto"/>.
    /// </summary>
    /// <param name="product">
    /// The product to map.
    /// </param>
    /// <returns>
    /// The mapped DTO.
    /// </returns>
    public static ProductDto ToDto(this Product product) => product.Adapt<ProductDto>(Config);

    /// <summary>
    /// Maps a <see cref="Customer"/> aggregate to a <see cref="CustomerDto"/>.
    /// </summary>
    /// <param name="customer">
    /// The customer to map.
    /// </param>
    /// <returns>
    /// The mapped DTO.
    /// </returns>
    public static CustomerDto ToDto(this Customer customer) => customer.Adapt<CustomerDto>(Config);

    /// <summary>
    /// Maps an <see cref="Order"/> aggregate, including its lines, to an <see cref="OrderDto"/>.
    /// </summary>
    /// <param name="order">
    /// The order to map.
    /// </param>
    /// <returns>
    /// The mapped DTO.
    /// </returns>
    public static OrderDto ToDto(this Order order)
    {
        var lines = order.Lines
            .Select(x => new OrderLineDto(x.ProductId, x.Sku, x.ProductName, x.Quantity, x.UnitPrice.Amount, x.DiscountPercent, x.LineTotal.Amount))
            .ToArray();

        return new(
            order.Id,
            order.CustomerId,
            order.CustomerName,
            order.CustomerPhone,
            order.CreatedAt,
            order.Status.ToString(),
            order.TotalQuantity,
            order.Total.Amount,
            order.Version,
            order.UpdatedAt,
            order.RejectionReason,
            lines);
    }

    private static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<Product, ProductDto>().Map(x => x.Price, x => x.Price.Amount);
        config.NewConfig<Customer, CustomerDto>();
        return config;
    }
}
