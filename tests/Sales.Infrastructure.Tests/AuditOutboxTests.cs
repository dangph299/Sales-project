using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sales.Domain;

namespace Sales.Infrastructure.Tests;

public sealed class AuditOutboxTests
{
    [Fact]
    public async Task Added_entity_creates_created_audit_outbox_message()
    {
        await using var fixture = await AuditSalesFixture.CreateAsync();
        await using var context = fixture.CreateContext();
        var product = Product.Create("sku-created", "Created", null, CategoryReferenceDataIds.Uncategorized);
        product.ClearDomainEvents();

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var audit = await fixture.SingleAuditAsync();
        Assert.Equal("Sales", audit.ServiceName);
        Assert.Equal("Product", audit.EntityType);
        Assert.Equal(product.Id.ToString(), audit.EntityId);
        Assert.Equal(AuditActions.Created, audit.Action);
        Assert.Contains(audit.Changes, change => change.PropertyPath == nameof(Product.Name) && ValueText(change.NewValue) == "Created");
    }

    [Fact]
    public async Task Modified_entity_records_only_changed_properties()
    {
        await using var fixture = await AuditSalesFixture.CreateAsync();
        var product = ProductTestFactory.CreatePublishedProduct("sku-modified", "Before", 100);
        product.ClearDomainEvents();
        await fixture.SeedProductAsync(product);

        await using var context = fixture.CreateContext();
        var loaded = await context.Products.SingleAsync(x => x.Id == product.Id);
        loaded.Update("After", loaded.Description, loaded.CategoryId);
        loaded.ClearDomainEvents();
        await context.SaveChangesAsync();

        var audit = await fixture.SingleAuditAsync();
        Assert.Equal(AuditActions.Updated, audit.Action);
        Assert.Equal([nameof(Product.Name)], audit.Changes.Select(change => change.PropertyPath).ToArray());
        Assert.Contains(audit.Changes, change => ValueText(change.OldValue) == "Before" && ValueText(change.NewValue) == "After");
    }

    [Fact]
    public async Task Soft_delete_creates_deleted_audit_event()
    {
        await using var fixture = await AuditSalesFixture.CreateAsync();
        var product = ProductTestFactory.CreatePublishedProduct("sku-deleted", "Deleted", 100);
        product.ClearDomainEvents();
        await fixture.SeedProductAsync(product);

        await using var context = fixture.CreateContext();
        var loaded = await context.Products.SingleAsync(x => x.Id == product.Id);
        loaded.Delete("tester");
        loaded.ClearDomainEvents();
        await context.SaveChangesAsync();

        var audit = await fixture.SingleAuditAsync();
        Assert.Equal(AuditActions.Deleted, audit.Action);
        Assert.Contains(audit.Changes, change => change.PropertyPath == nameof(Product.IsDelete) && ValueText(change.NewValue) == "True");
    }

    [Fact]
    public async Task Masked_property_does_not_expose_plain_value()
    {
        await using var fixture = await AuditSalesFixture.CreateAsync();
        await using var context = fixture.CreateContext();
        var customer = Customer.Create("Nguyen Van A", "0901234567");
        customer.ClearDomainEvents();

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var audit = await fixture.SingleAuditAsync();
        Assert.Contains(audit.Changes, change => change.PropertyPath == nameof(Customer.Phone) && ValueText(change.NewValue) == "***");
        Assert.DoesNotContain(audit.Changes, change => ValueText(change.NewValue) == "0901234567");
    }

    [Fact]
    public async Task Outbox_entity_is_not_audited()
    {
        await using var fixture = await AuditSalesFixture.CreateAsync();
        await using var context = fixture.CreateContext();
        var envelope = EventEnvelopeFactory.Create(Guid.NewGuid(), 1, new StockReserved(Guid.NewGuid()));
        context.OutboxMessages.Add(OutboxMessage.From(envelope, KafkaTopics.StockReserved));

        await context.SaveChangesAsync();

        Assert.Equal(1, await context.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task Order_line_changes_are_grouped_under_order()
    {
        await using var fixture = await AuditSalesFixture.CreateAsync();
        var product = ProductTestFactory.CreatePublishedProduct("sku-order", "Order Product", 100);
        product.ClearDomainEvents();
        var order = Order.Create(
            CustomerSnapshot.Create(Guid.NewGuid(), "Customer", "0901234567"),
            [new(ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, true), 1, 0)]);
        order.ClearDomainEvents();
        await fixture.SeedProductAndOrderAsync(product, order);

        await using var context = fixture.CreateContext();
        var loaded = await context.Orders.Include(x => x.Lines).SingleAsync(x => x.Id == order.Id);
        loaded.ReplaceLines([new(ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, true), 4, 0)]);
        loaded.ClearDomainEvents();
        await context.SaveChangesAsync();

        var audit = await fixture.SingleAuditAsync();
        Assert.Equal("Order", audit.EntityType);
        Assert.Equal(order.Id.ToString(), audit.EntityId);
        Assert.Contains(audit.Changes, change => change.PropertyPath.Contains("Lines[ProductId=", StringComparison.Ordinal)
            && change.PropertyPath.EndsWith(".Quantity", StringComparison.Ordinal)
            && ValueText(change.NewValue) == "4");
    }

    [Fact]
    public async Task Rollback_discards_business_data_and_audit_outbox()
    {
        await using var fixture = await AuditSalesFixture.CreateAsync();
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var product = ProductTestFactory.CreatePublishedProduct("sku-rollback", "Rollback", 100);
        product.ClearDomainEvents();
        context.Products.Add(product);
        await context.SaveChangesAsync();

        await transaction.RollbackAsync();

        await using var verify = fixture.CreateContext();
        Assert.False(await verify.Products.AnyAsync(x => x.Id == product.Id));
        Assert.False(await verify.OutboxMessages.AnyAsync());
    }

    private sealed class AuditSalesFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<SalesDbContext> _options;
        private readonly TestExecutionContext _executionContext = new();

        private AuditSalesFixture(SqliteConnection connection, DbContextOptions<SalesDbContext> options)
        {
            _connection = connection;
            _options = options;
        }

        public static async Task<AuditSalesFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var auditOptions = Options.Create(new AuditOptions
            {
                ServiceName = "Sales",
                TopicName = KafkaTopics.SalesAudit
            });
            auditOptions.Value.IgnoreEntity<OutboxMessage>();
            auditOptions.Value.IgnoreEntity<InboxMessage>();
            var signal = new OutboxSignal();
            var interceptor = new AuditSaveChangesInterceptor(
                new EfCoreAuditEntryFactory(
                    auditOptions,
                    new StaticAuditContextAccessor(),
                    new SalesAuditAggregateResolver(),
                    [new OrderAuditEnricher()]),
                auditOptions,
                signal);
            var options = new DbContextOptionsBuilder<SalesDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(interceptor)
                .Options;
            var fixture = new AuditSalesFixture(connection, options);
            await using var context = fixture.CreateContext();
            await context.Database.EnsureCreatedAsync();
            return fixture;
        }

        public SalesDbContext CreateContext()
        {
            return new SalesDbContext(_options, _executionContext);
        }

        public async Task SeedProductAsync(Product product)
        {
            await using var context = CreateContext();
            context.Products.Add(product);
            await context.SaveChangesAsync();
            context.OutboxMessages.RemoveRange(context.OutboxMessages);
            await context.SaveChangesAsync();
        }

        public async Task SeedProductAndOrderAsync(Product product, Order order)
        {
            await using var context = CreateContext();
            context.Products.Add(product);
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            context.OutboxMessages.RemoveRange(context.OutboxMessages);
            await context.SaveChangesAsync();
        }

        public async Task<AuditLogEvent> SingleAuditAsync()
        {
            await using var context = CreateContext();
            var outbox = await context.OutboxMessages.SingleAsync();
            var envelope = JsonSerializer.Deserialize<EventEnvelope>(outbox.Payload)!;
            return envelope.Data.Deserialize<AuditLogEvent>()!;
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }

    private sealed class StaticAuditContextAccessor : IAuditContextAccessor
    {
        public string? ActorId => "tester";
        public string? ActorName => "tester";
        public string? CorrelationId => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa").ToString();
        public string? CausationId => null;
        public string? TraceId => "trace";
    }

    private static string? ValueText(object? value)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.True => "True",
                JsonValueKind.False => "False",
                _ => jsonElement.ToString()
            };
        }

        return value?.ToString();
    }
}
