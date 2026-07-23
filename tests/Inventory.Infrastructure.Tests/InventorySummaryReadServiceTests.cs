using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Domain;
using MapsterMapper;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Inventory.Infrastructure.Tests;

/// <summary>
/// End-to-end reliability tests for the inventory summary aggregate query against a real PostgreSQL.
/// </summary>
[Trait("Category", "Reliability")]
[Collection("InventoryReliabilityPostgres")]
public sealed class InventorySummaryReadServiceTests
{
    private readonly InventoryPostgresReliabilityFixture _fixture;

    public InventorySummaryReadServiceTests(InventoryPostgresReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task GetSummaryAsync_aggregates_counts_by_threshold()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var db = NewContext();
        await db.Database.MigrateAsync();
        await db.Items.ExecuteDeleteAsync();

        await SeedAvailabilities(db, 0, 3, 5, 6, 10);

        var sut = new InventoryReadService(db, NewMapper());

        var summary = await sut.GetSummaryAsync(new InventorySummaryFilter(LowStockThreshold: 5));

        Assert.Equal(5, summary.TotalItems);
        Assert.Equal(24, summary.TotalQuantity);
        Assert.Equal(1, summary.OutOfStock);
        Assert.Equal(2, summary.LowStock);
        Assert.Equal(2, summary.InStock);
        Assert.Equal(5, summary.LowStockThreshold);
    }

    [SkippableFact]
    public async Task GetSummaryAsync_returns_zeros_when_empty()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var db = NewContext();
        await db.Database.MigrateAsync();
        await db.Items.ExecuteDeleteAsync();

        var sut = new InventoryReadService(db, NewMapper());

        var summary = await sut.GetSummaryAsync(new InventorySummaryFilter(5));

        Assert.Equal(0, summary.TotalItems);
        Assert.Equal(0, summary.TotalQuantity);
    }

    private static async Task SeedAvailabilities(InventoryDbContext db, params int[] availabilities)
    {
        var items = availabilities.Select(
            (available, index) => InventoryItem.Create(Guid.NewGuid(), $"sku-{index}", available));
        db.Items.AddRange(items);
        await db.SaveChangesAsync();
    }

    private static IMapper NewMapper() => new Mapper(new TypeAdapterConfig());

    private InventoryDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new InventoryDbContext(options);
    }
}
