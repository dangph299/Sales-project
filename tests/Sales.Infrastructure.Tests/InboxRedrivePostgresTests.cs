using BuildingBlocks.Application;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sales.Application.Common.Interfaces;
using Sales.Application.Features.Orders.Realtime;
using Sales.Domain;
using Xunit;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// End-to-end reliability test for the durable inbox re-drive (H1) against a real PostgreSQL: a
/// StockReserved event whose first processing failed is replayed through the real
/// <see cref="SalesInventoryEventProcessor"/> and advances the order to Confirmed. Because KafkaFlow
/// commits the consumer offset even on handler failure, this re-drive — not Kafka — is the retry path.
/// </summary>
[Trait("Category", "Reliability")]
[Collection("SalesReliabilityPostgres")]
public sealed class InboxRedrivePostgresTests
{
    private readonly PostgresReliabilityFixture _fixture;

    public InboxRedrivePostgresTests(PostgresReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Failed_stock_reserved_event_is_redriven_and_confirms_the_order()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var provider = BuildProvider();
        await ResetDatabaseAsync(provider);

        // An order awaiting inventory, and a StockReserved reply whose first processing already failed.
        var orderId = await SeedPendingInventoryOrderAsync(provider);
        var reservedEnvelope = EventEnvelopeFactory.Create(orderId, 2L, new StockReserved(orderId), "inventory");
        await SeedFailedInboxAsync(provider, reservedEnvelope);

        var redrive = CreateRedriveService(provider);
        await redrive.RunRedriveCycleAsync();

        await using var verifyScope = provider.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<SalesDbContext>();
        var order = await db.Orders.AsNoTracking().SingleAsync(x => x.Id == orderId);
        Assert.Equal(OrderStatus.Confirmed, order.Status);

        var inboxRow = await db.InboxMessages.AsNoTracking().SingleAsync(x => x.EventId == reservedEnvelope.EventId);
        Assert.Equal(InboxMessageStatus.Processed, inboxRow.Status);
    }

    private async Task<Guid> SeedPendingInventoryOrderAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var customer = OrderCustomerSnapshot.Create(Guid.NewGuid(), "Nguyen Van A", "0901234567", null, null);
        var product = ProductSnapshot.Create(Guid.NewGuid(), "sku-1", "Keyboard", Money.Vnd(100_000), true);
        var order = Order.Create(OrderTestFactory.NextOrderCode(), customer, [new OrderLineItem(product, 1, 0m)]);
        order.RequestConfirmation();

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    private async Task SeedFailedInboxAsync(ServiceProvider provider, EventEnvelope envelope)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        db.InboxMessages.Add(new InboxMessage
        {
            EventId = envelope.EventId,
            Status = InboxMessageStatus.Failed,
            Attempts = 1,
            ProcessedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            Payload = System.Text.Json.JsonSerializer.Serialize(envelope),
            Consumer = "sales-v1",
            OriginalTopic = KafkaTopics.StockReserved,
            OriginalConsumerGroup = KafkaConsumerGroups.SalesInventoryResults
        });
        await db.SaveChangesAsync();
    }

    private SalesInboxRedriveService CreateRedriveService(ServiceProvider provider)
    {
        return new SalesInboxRedriveService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<SalesInboxRedriveService>>(),
            provider.GetRequiredService<IClock>(),
            provider.GetRequiredService<IOptions<InboxConsumerOptions>>());
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<SalesDbContext>(options => options.UseNpgsql(_fixture.ConnectionString));
        services.AddSingleton<IExecutionContext, TestExecutionContext>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IOutboxSignal, OutboxSignal>();
        services.AddSingleton<IOrderRealtimeNotifier, NoopOrderRealtimeNotifier>();
        services.AddScoped<IIntegrationEventProcessor, SalesInventoryEventProcessor>();
        services.AddScoped<IInboxFailureRecorder, SalesInboxFailureRecorder>();
        services.AddSingleton<IOptions<InboxConsumerOptions>>(Options.Create(new InboxConsumerOptions()));
        return services.BuildServiceProvider();
    }

    private static async Task ResetDatabaseAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        await db.Database.MigrateAsync();
        await db.InboxMessages.ExecuteDeleteAsync();
        await db.OutboxMessages.ExecuteDeleteAsync();
        await db.Orders.ExecuteDeleteAsync();
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";

        public Guid CorrelationId => Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    }

    private sealed class NoopOrderRealtimeNotifier : IOrderRealtimeNotifier
    {
        public Task NotifyOrderStatusChangedAsync(
            OrderStatusChangedNotification notification,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
