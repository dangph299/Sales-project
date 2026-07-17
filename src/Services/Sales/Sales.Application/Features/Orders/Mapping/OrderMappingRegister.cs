using Mapster;
using Sales.Application.Features.Orders.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Mapping;

/// <summary>
/// Mapping configuration owned by the <see cref="Order"/> aggregate root, including its
/// <see cref="OrderLine"/> entities, which have no lifecycle outside the aggregate.
/// </summary>
public sealed class OrderMappingRegister : IRegister
{
    /// <inheritdoc/>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<OrderLine, OrderLineDto>()
            .Map(
                destination => destination.UnitPrice,
                source => source.UnitPrice.Amount)
            .Map(
                destination => destination.LineTotal,
                source => source.LineTotal.Amount);

        config.NewConfig<Order, OrderDto>()
            .Map(
                destination => destination.Status,
                source => source.Status.ToString())
            .Map(
                destination => destination.Total,
                source => source.Total.Amount)
            .Map(
                destination => destination.Lines,
                source => source.Lines);
    }
}
