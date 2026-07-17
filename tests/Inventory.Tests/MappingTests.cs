using BuildingBlocks.Application.Mapping;
using Inventory.Application;
using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.Reservations.DTOs;
using Inventory.Domain;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Tests;

public sealed class MappingTests
{
    private static readonly IMapper Mapper = InventoryMapperFactory.Create();

    [Fact]
    public void Scanning_the_application_assembly_discovers_the_mapping_registers()
    {
        var services = new ServiceCollection();
        services.AddApplicationMapping(typeof(DependencyInjection).Assembly);
        var config = services.BuildServiceProvider().GetRequiredService<TypeAdapterConfig>();

        Assert.NotEmpty(config.RuleMap);
    }

    [Fact]
    public void Every_registered_mapping_compiles()
    {
        var services = new ServiceCollection();
        services.AddApplicationMapping(typeof(DependencyInjection).Assembly);
        var config = services.BuildServiceProvider().GetRequiredService<TypeAdapterConfig>();

        config.Compile();
    }

    [Fact]
    public void Inventory_item_maps_its_stock_levels_and_version()
    {
        var productId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, " sku-1 ", 10);
        item.Reserve(4);

        var snapshot = Mapper.Map<InventorySnapshot>(item);

        Assert.Equal(productId, snapshot.ProductId);
        Assert.Equal("SKU-1", snapshot.Sku);
        Assert.Equal(6, snapshot.Available);
        Assert.Equal(4, snapshot.Reserved);
        Assert.Equal(item.Version, snapshot.Version);
    }

    [Fact]
    public void Reservation_maps_its_status_as_a_string_and_projects_its_lines()
    {
        var orderId = Guid.NewGuid();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var reservation = Reservation.Create(orderId, 1, [
            new ReservationRequestLine(firstProductId, "sku-1", 2),
            new ReservationRequestLine(secondProductId, "sku-2", 3)
        ]);

        var snapshot = Mapper.Map<ReservationSnapshot>(reservation);

        Assert.Equal(orderId, snapshot.OrderId);
        Assert.Equal("Active", snapshot.Status);
        Assert.Equal(reservation.CreatedAt, snapshot.CreatedAt);
        Assert.Equal(2, snapshot.Lines.Count);

        var firstLine = snapshot.Lines.Single(x => x.ProductId == firstProductId);
        Assert.Equal("SKU-1", firstLine.Sku);
        Assert.Equal(2, firstLine.Quantity);
        Assert.Equal(3, snapshot.Lines.Single(x => x.ProductId == secondProductId).Quantity);
    }

    [Fact]
    public void Released_reservation_maps_its_status_as_a_string()
    {
        var reservation = Reservation.Create(Guid.NewGuid(), 1, [new ReservationRequestLine(Guid.NewGuid(), "sku-1", 2)]);
        reservation.Release(2);

        var snapshot = Mapper.Map<ReservationSnapshot>(reservation);

        Assert.Equal("Released", snapshot.Status);
    }

    [Fact]
    public void Line_less_tombstone_reservation_maps_to_an_empty_line_collection()
    {
        var reservation = Reservation.CreateReleasedTombstone(Guid.NewGuid(), 1);

        var snapshot = Mapper.Map<ReservationSnapshot>(reservation);

        Assert.Equal("Released", snapshot.Status);
        Assert.Empty(snapshot.Lines);
    }
}
