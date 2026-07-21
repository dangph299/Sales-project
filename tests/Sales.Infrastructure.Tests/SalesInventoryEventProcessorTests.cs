using System.Text.Json;
using BuildingBlocks.Application;
using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sales.Application.Features.Orders.Realtime;
using Sales.Domain;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Pins which consumed Inventory events persist Sales state. The processor decides this from a typed
/// transition rather than by comparing its own outcome text, and these tests fail if that decision
/// ever moves back onto the returned string.
/// </summary>
public sealed class SalesInventoryEventProcessorTests
{
    private static readonly DateTimeOffset ConsumedAt = new(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Stock_reserved_confirms_the_order_and_commits()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        var order = await fixture.SeedPendingInventoryOrderAsync();

        var outcome = await fixture.ProcessAsync(Envelope(nameof(StockReserved), order.Id));

        Assert.Equal("Reserved", outcome);
        await using var verify = fixture.CreateContext();
        Assert.Equal(OrderStatus.Confirmed, (await verify.Orders.SingleAsync(o => o.Id == order.Id)).Status);
        var notification = Assert.Single(fixture.Notifier.Notifications);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal(OrderStatus.PendingInventory, notification.PreviousStatus);
        Assert.Equal(OrderStatus.Confirmed, notification.CurrentStatus);
    }

    [Fact]
    public async Task Stock_rejected_records_the_rejection_reason_and_commits()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        var order = await fixture.SeedPendingInventoryOrderAsync();
        var envelope = Envelope(
            nameof(StockRejected),
            order.Id,
            new StockRejected(order.Id, "out of stock"));

        var outcome = await fixture.ProcessAsync(envelope);

        Assert.Equal("Rejected", outcome);
        await using var verify = fixture.CreateContext();
        var rejectedOrder = await verify.Orders.SingleAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.InventoryRejected, rejectedOrder.Status);
        Assert.Equal("out of stock", rejectedOrder.RejectionReason);
        var notification = Assert.Single(fixture.Notifier.Notifications);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal(OrderStatus.PendingInventory, notification.PreviousStatus);
        Assert.Equal(OrderStatus.InventoryRejected, notification.CurrentStatus);
    }

    [Fact]
    public async Task Unknown_event_type_leaves_the_order_untouched()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        var order = await fixture.SeedPendingInventoryOrderAsync();

        var outcome = await fixture.ProcessAsync(Envelope("SomethingSalesDoesNotHandle", order.Id));

        Assert.Equal("Ignored", outcome);
        await using var verify = fixture.CreateContext();
        Assert.Equal(OrderStatus.PendingInventory, (await verify.Orders.SingleAsync(o => o.Id == order.Id)).Status);
        Assert.Empty(fixture.Notifier.Notifications);
    }

    [Fact]
    public async Task Already_processed_event_is_skipped_without_touching_the_order()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        var order = await fixture.SeedPendingInventoryOrderAsync();
        var envelope = Envelope(nameof(StockReserved), order.Id);
        await fixture.SeedProcessedInboxAsync(envelope.EventId);

        var outcome = await fixture.ProcessAsync(envelope);

        Assert.Equal("Duplicate", outcome);
        await using var verify = fixture.CreateContext();
        Assert.Equal(OrderStatus.PendingInventory, (await verify.Orders.SingleAsync(o => o.Id == order.Id)).Status);
        Assert.Empty(fixture.Notifier.Notifications);
    }

    [Fact]
    public async Task Redelivered_event_is_applied_once_however_many_times_it_arrives()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        var order = await fixture.SeedPendingInventoryOrderAsync();
        var envelope = Envelope(nameof(StockReserved), order.Id);

        var firstOutcome = await fixture.ProcessAsync(envelope);
        var secondOutcome = await fixture.ProcessAsync(envelope);

        Assert.Equal("Reserved", firstOutcome);
        Assert.Equal("Duplicate", secondOutcome);
        await using var verify = fixture.CreateContext();
        Assert.Equal(OrderStatus.Confirmed, (await verify.Orders.SingleAsync(o => o.Id == order.Id)).Status);
        Assert.Equal(1, await verify.InboxMessages.CountAsync(inboxMessage => inboxMessage.EventId == envelope.EventId));
        Assert.Single(fixture.Notifier.Notifications);
    }

    [Fact]
    public async Task Retried_event_that_changes_state_persists_the_inbox_transition_to_processed()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        var order = await fixture.SeedPendingInventoryOrderAsync();
        var envelope = Envelope(nameof(StockReserved), order.Id);
        await fixture.SeedFailedInboxAsync(envelope.EventId);

        var outcome = await fixture.ProcessAsync(envelope);

        Assert.Equal("Reserved", outcome);
        await using var verify = fixture.CreateContext();
        var inboxMessage = await verify.InboxMessages.SingleAsync(row => row.EventId == envelope.EventId);
        Assert.Equal(InboxMessageStatus.Processed, inboxMessage.Status);
    }

    [Fact]
    public async Task Retried_event_that_changes_nothing_is_still_marked_processed()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        var order = await fixture.SeedPendingInventoryOrderAsync();
        var envelope = Envelope("SomethingSalesDoesNotHandle", order.Id);
        await fixture.SeedFailedInboxAsync(envelope.EventId);

        var outcome = await fixture.ProcessAsync(envelope);

        // An event Sales has no handler for was still processed successfully. Its inbox row must be
        // persisted as Processed, otherwise the re-drive service replays it on every cycle forever.
        Assert.Equal("Ignored", outcome);
        await using var verify = fixture.CreateContext();
        var inboxMessage = await verify.InboxMessages.SingleAsync(row => row.EventId == envelope.EventId);
        Assert.Equal(InboxMessageStatus.Processed, inboxMessage.Status);
    }

    [Fact]
    public async Task Retried_event_that_changes_nothing_is_not_redriven_again()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        var order = await fixture.SeedPendingInventoryOrderAsync();
        var envelope = Envelope("SomethingSalesDoesNotHandle", order.Id);
        await fixture.SeedFailedInboxAsync(envelope.EventId);

        await fixture.ProcessAsync(envelope);
        var secondOutcome = await fixture.ProcessAsync(envelope);

        // The re-drive query only picks up rows still in Failed state, so a row that reports
        // Duplicate on the next delivery can no longer be selected for replay.
        Assert.Equal("Duplicate", secondOutcome);
        await using var verify = fixture.CreateContext();
        Assert.Empty(await verify.InboxMessages
            .Where(row => row.Status == InboxMessageStatus.Failed)
            .ToListAsync());
    }

    [Fact]
    public async Task Retried_event_for_an_unknown_order_is_marked_processed_so_it_stops_being_redriven()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        var envelope = Envelope(nameof(StockReserved), Guid.NewGuid());
        await fixture.SeedFailedInboxAsync(envelope.EventId);

        var outcome = await fixture.ProcessAsync(envelope);

        Assert.Equal(ErrorCodes.OrderNotFound, outcome);
        await using var verify = fixture.CreateContext();
        var inboxMessage = await verify.InboxMessages.SingleAsync(row => row.EventId == envelope.EventId);
        Assert.Equal(InboxMessageStatus.Processed, inboxMessage.Status);
    }

    [Fact]
    public async Task Event_for_an_unknown_order_is_recorded_so_redelivery_is_still_skipped()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        var envelope = Envelope(nameof(StockReserved), Guid.NewGuid());

        var outcome = await fixture.ProcessAsync(envelope);

        Assert.Equal(ErrorCodes.OrderNotFound, outcome);
        await using var verify = fixture.CreateContext();
        Assert.True(await verify.InboxMessages.AnyAsync(inboxMessage => inboxMessage.EventId == envelope.EventId));
    }

    [Fact]
    public async Task Event_that_fails_processing_leaves_the_inbox_row_retryable()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        // A Draft order cannot be marked reserved, so the domain rejects this event.
        var order = await fixture.SeedDraftOrderAsync();
        var envelope = Envelope(nameof(StockReserved), order.Id);
        await fixture.SeedFailedInboxAsync(envelope.EventId);

        await Assert.ThrowsAsync<DomainException>(() => fixture.ProcessAsync(envelope));

        // A real processing failure must still roll back, so the re-drive service keeps the row.
        await using var verify = fixture.CreateContext();
        var inboxMessage = await verify.InboxMessages.SingleAsync(row => row.EventId == envelope.EventId);
        Assert.Equal(InboxMessageStatus.Failed, inboxMessage.Status);
    }

    [Fact]
    public async Task First_delivery_that_fails_processing_records_no_inbox_row()
    {
        await using var fixture = await SalesInventoryFixture.CreateAsync();
        var order = await fixture.SeedDraftOrderAsync();
        var envelope = Envelope(nameof(StockReserved), order.Id);

        await Assert.ThrowsAsync<DomainException>(() => fixture.ProcessAsync(envelope));

        await using var verify = fixture.CreateContext();
        Assert.Empty(await verify.InboxMessages.ToListAsync());
    }

    private static EventEnvelope Envelope(string eventType, Guid orderId, object? data = null)
    {
        return new EventEnvelope(
            EventId: Guid.NewGuid(),
            EventType: eventType,
            AggregateId: orderId,
            Version: 1,
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAt: ConsumedAt,
            Actor: "inventory-test",
            Data: JsonSerializer.SerializeToElement(data ?? new { OrderId = orderId }));
    }

    private sealed class SalesInventoryFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly DbContextOptions<SalesDbContext> options;
        private readonly TestExecutionContext executionContext = new();
        public RecordingOrderRealtimeNotifier Notifier { get; } = new();

        private SalesInventoryFixture(SqliteConnection connection, DbContextOptions<SalesDbContext> options)
        {
            this.connection = connection;
            this.options = options;
        }

        public static async Task<SalesInventoryFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SalesDbContext>()
                .UseSqlite(connection)
                .Options;
            var fixture = new SalesInventoryFixture(connection, options);
            await using var context = fixture.CreateContext();
            await context.Database.EnsureCreatedAsync();
            return fixture;
        }

        public SalesDbContext CreateContext()
        {
            return new SalesDbContext(options, executionContext);
        }

        public async Task<Order> SeedDraftOrderAsync()
        {
            return await SeedOrderAsync(requestConfirmation: false);
        }

        public async Task<Order> SeedPendingInventoryOrderAsync()
        {
            return await SeedOrderAsync(requestConfirmation: true);
        }

        private async Task<Order> SeedOrderAsync(bool requestConfirmation)
        {
            var product = ProductTestFactory.CreatePublishedProduct("sku-inventory-event", "Inventory event", 100);
            var customer = CustomerSnapshot.Create(Guid.NewGuid(), "Consumer", "0912345678");
            var productSnapshot = ProductSnapshot.Create(
                product.Id,
                product.Sku,
                product.Name,
                ProductTestFactory.PrimaryVariant(product).Price,
                isActive: true);
            var order = Order.Create(customer, [new OrderLineItem(productSnapshot, 1, 0)]);
            if (requestConfirmation)
            {
                order.RequestConfirmation();
            }

            order.ClearDomainEvents();
            product.ClearDomainEvents();

            await using var context = CreateContext();
            context.Products.Add(product);
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            return order;
        }

        public async Task SeedProcessedInboxAsync(Guid eventId)
        {
            await using var context = CreateContext();
            context.InboxMessages.Add(InboxMessage.Create(eventId, ConsumedAt, consumer: "sales-v1"));
            await context.SaveChangesAsync();
        }

        public async Task SeedFailedInboxAsync(Guid eventId)
        {
            await using var context = CreateContext();
            var inboxMessage = InboxMessage.Create(eventId, ConsumedAt, consumer: "sales-v1");
            inboxMessage.Status = InboxMessageStatus.Failed;
            inboxMessage.Attempts = 1;
            context.InboxMessages.Add(inboxMessage);
            await context.SaveChangesAsync();
        }

        public async Task<string> ProcessAsync(EventEnvelope envelope)
        {
            await using var context = CreateContext();
            var processor = new SalesInventoryEventProcessor(
                context,
                new FixedClock(ConsumedAt),
                Notifier,
                NullLogger<SalesInventoryEventProcessor>.Instance);
            return await processor.ProcessAsync(envelope);
        }

        public async ValueTask DisposeAsync()
        {
            await connection.DisposeAsync();
        }
    }

    private sealed class FixedClock(DateTimeOffset currentUtc) : IClock
    {
        public DateTimeOffset UtcNow => currentUtc;
    }

    public sealed class RecordingOrderRealtimeNotifier : IOrderRealtimeNotifier
    {
        public List<OrderStatusChangedNotification> Notifications { get; } = [];

        public Task NotifyOrderStatusChangedAsync(
            OrderStatusChangedNotification notification,
            CancellationToken cancellationToken)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }
    }
}
