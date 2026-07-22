using BuildingBlocks.Contracts;
using Sales.Application.Common.Interfaces;
using Sales.Application.Features.Customers.Interfaces;
using Sales.Application.Features.Orders.Commands;
using Sales.Application.Features.Orders.DTOs;
using Sales.Application.Features.Orders.Interfaces;
using Sales.Domain;

namespace Sales.Application.Tests;

/// <summary>
/// Covers what the create and update flows are each allowed to touch. The point of splitting them
/// is that creating an order may write a customer while editing one may not, so these tests assert
/// on the customer table as much as on the order.
/// </summary>
public sealed class OrderCustomerSnapshotHandlerTests
{
    [Fact]
    public async Task Create_reuses_the_customer_already_holding_that_phone_number()
    {
        var existingCustomer = Customer.Create("CUS001", "Nguyen Van A", "0901234567");
        var customerRepository = new FakeCustomerRepository(existingCustomer);
        var handler = CreateHandler(customerRepository, out var product);

        var dto = await handler.Handle(
            new CreateOrder(
                new CreateOrderCustomer("0901234567", "Nguyen Van A"),
                [new OrderLineInput(ProductTestFactory.PrimaryVariant(product).Id, 1, 0)]),
            CancellationToken.None);

        Assert.Empty(customerRepository.AddedCustomers);
        Assert.Equal(existingCustomer.Id, dto.CustomerId);
    }

    [Fact]
    public async Task Create_records_the_requested_name_even_when_the_customer_is_known_under_another()
    {
        var existingCustomer = Customer.Create("CUS001", "Nguyen Van A", "0901234567");
        var customerRepository = new FakeCustomerRepository(existingCustomer);
        var handler = CreateHandler(customerRepository, out var product);

        var dto = await handler.Handle(
            new CreateOrder(
                new CreateOrderCustomer("0901234567", "Nguyen Van B"),
                [new OrderLineInput(ProductTestFactory.PrimaryVariant(product).Id, 1, 0)]),
            CancellationToken.None);

        Assert.Equal("Nguyen Van B", dto.CustomerName);
        Assert.Equal("Nguyen Van A", existingCustomer.Name);
    }

    [Fact]
    public async Task Create_adds_a_customer_when_the_phone_number_is_new()
    {
        var customerRepository = new FakeCustomerRepository(null);
        var handler = CreateHandler(customerRepository, out var product);

        var dto = await handler.Handle(
            new CreateOrder(
                new CreateOrderCustomer("0912-345-678", "Nguyen Van C", "c@example.com", "12 Le Loi"),
                [new OrderLineInput(ProductTestFactory.PrimaryVariant(product).Id, 1, 0)]),
            CancellationToken.None);

        var newCustomer = Assert.Single(customerRepository.AddedCustomers);
        Assert.Equal("0912345678", newCustomer.NormalizedPhone);
        Assert.Equal(newCustomer.Id, dto.CustomerId);
        Assert.Equal("0912-345-678", dto.CustomerPhone);
        Assert.Equal("c@example.com", dto.CustomerEmail);
    }

    [Fact]
    public async Task Create_takes_the_phone_lock_before_looking_the_customer_up()
    {
        // Ordering is the whole defence against two concurrent creates both deciding the customer
        // is missing, so it is asserted rather than assumed.
        var customerRepository = new FakeCustomerRepository(null);
        var handler = CreateHandler(customerRepository, out var product);

        await handler.Handle(
            new CreateOrder(
                new CreateOrderCustomer("0912345678", "Nguyen Van C"),
                [new OrderLineInput(ProductTestFactory.PrimaryVariant(product).Id, 1, 0)]),
            CancellationToken.None);

        Assert.Equal(["lock", "find"], customerRepository.CallLog.Take(2));
    }

    [Fact]
    public async Task Create_writes_the_customer_and_the_order_in_one_committed_transaction()
    {
        var customerRepository = new FakeCustomerRepository(null);
        var transactionManager = new FakeSalesTransactionManager();
        var unitOfWork = new FakeSalesUnitOfWork();
        var handler = CreateHandler(customerRepository, out var product, transactionManager, unitOfWork);

        await handler.Handle(
            new CreateOrder(
                new CreateOrderCustomer("0912345678", "Nguyen Van C"),
                [new OrderLineInput(ProductTestFactory.PrimaryVariant(product).Id, 1, 0)]),
            CancellationToken.None);

        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal(1, transactionManager.CommitCount);
        Assert.Equal(0, transactionManager.RollbackCount);
    }

    [Fact]
    public async Task Create_rolls_back_the_new_customer_when_saving_the_order_fails()
    {
        var customerRepository = new FakeCustomerRepository(null);
        var transactionManager = new FakeSalesTransactionManager();
        var unitOfWork = new FakeSalesUnitOfWork { FailOnSave = new InvalidOperationException("save failed") };
        var handler = CreateHandler(customerRepository, out var product, transactionManager, unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new CreateOrder(
                new CreateOrderCustomer("0912345678", "Nguyen Van C"),
                [new OrderLineInput(ProductTestFactory.PrimaryVariant(product).Id, 1, 0)]),
            CancellationToken.None));

        Assert.Equal(0, transactionManager.CommitCount);
        Assert.Equal(1, transactionManager.RollbackCount);
    }

    [Fact]
    public async Task Create_rethrows_a_unique_violation_that_was_not_a_concurrent_customer_insert()
    {
        // The retry exists for one specific race. A unique violation with no customer behind it —
        // a customer code collision, say — must surface, not be retried into a different error.
        var customerRepository = new FakeCustomerRepository(null);
        var unitOfWork = new FakeSalesUnitOfWork { FailOnSave = new InvalidOperationException("duplicate key") };
        var handler = CreateHandler(
            customerRepository,
            out var product,
            unitOfWork: unitOfWork,
            persistenceExceptionClassifier: new AlwaysUniqueViolationClassifier());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new CreateOrder(
                new CreateOrderCustomer("0912345678", "Nguyen Van C"),
                [new OrderLineInput(ProductTestFactory.PrimaryVariant(product).Id, 1, 0)]),
            CancellationToken.None));

        Assert.Equal("duplicate key", exception.Message);
    }

    [Fact]
    public async Task Update_rewrites_the_order_snapshot_without_touching_the_customer()
    {
        var customer = Customer.Create("CUS001", "Nguyen Van A", "0901234567");
        var order = CreateDraftOrder(customer.Id);
        var handler = new UpdateOrderCustomerHandler(
            new FakeOrderRepository(order),
            new FakeSalesUnitOfWork(),
            new RecordingLogger<UpdateOrderCustomerHandler>(),
            SalesMapperFactory.Create());

        var dto = await handler.Handle(
            new UpdateOrderCustomer(order.Id, order.Version, "Nguyen Van B", "0912-345-678", null, null),
            CancellationToken.None);

        Assert.Equal("Nguyen Van B", dto.CustomerName);
        Assert.Equal("0912-345-678", dto.CustomerPhone);
        Assert.Equal("0912345678", order.NormalizedCustomerPhone);
        Assert.Equal("8765432190", order.ReversedCustomerPhone);

        // The customer is untouched, which is the entire point of the split.
        Assert.Equal("Nguyen Van A", customer.Name);
        Assert.Equal("0901234567", customer.Phone);
        Assert.Equal("0901234567", customer.NormalizedPhone);
    }

    [Fact]
    public async Task Update_keeps_the_customer_the_order_was_placed_for()
    {
        var originalCustomerId = Guid.NewGuid();
        var order = CreateDraftOrder(originalCustomerId);
        var handler = new UpdateOrderCustomerHandler(
            new FakeOrderRepository(order),
            new FakeSalesUnitOfWork(),
            new RecordingLogger<UpdateOrderCustomerHandler>(),
            SalesMapperFactory.Create());

        var dto = await handler.Handle(
            new UpdateOrderCustomer(order.Id, order.Version, "Nguyen Van B", "0987654321", null, null),
            CancellationToken.None);

        Assert.Equal(originalCustomerId, dto.CustomerId);
    }

    [Fact]
    public void Update_handler_cannot_reach_the_customer_table_at_all()
    {
        // Structural rather than behavioural: with no customer repository among its dependencies,
        // this handler has no way to write Customer even if someone later adds the call.
        var constructorParameterTypes = typeof(UpdateOrderCustomerHandler)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(x => x.ParameterType)
            .ToArray();

        Assert.DoesNotContain(typeof(ICustomerRepository), constructorParameterTypes);
        Assert.DoesNotContain(typeof(IRepository<Customer>), constructorParameterTypes);
    }

    private static CreateOrderHandler CreateHandler(
        FakeCustomerRepository customerRepository,
        out Product product,
        FakeSalesTransactionManager? transactionManager = null,
        FakeSalesUnitOfWork? unitOfWork = null,
        IPersistenceExceptionClassifier? persistenceExceptionClassifier = null)
    {
        product = ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        return new CreateOrderHandler(
            new FakeOrderRepository(null),
            customerRepository,
            new FakeProductRepository(product),
            transactionManager ?? new FakeSalesTransactionManager(),
            unitOfWork ?? new FakeSalesUnitOfWork(),
            new FakeCodeGenerator("CUS001"),
            new FakeCodeGenerator("ORD-0000001"),
            persistenceExceptionClassifier ?? new NeverClassifyingClassifier(),
            new RecordingLogger<CreateOrderHandler>(),
            SalesMapperFactory.Create());
    }

    private static Order CreateDraftOrder(Guid customerId)
    {
        var product = ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        var variant = ProductTestFactory.PrimaryVariant(product);
        return Order.Create(
            "ORD-0000001",
            OrderCustomerSnapshot.Create(customerId, "Nguyen Van A", "0901234567", null, null),
            [
                new OrderLineItem(
                    ProductSnapshot.Create(
                        product.Id,
                        variant.Id,
                        product.ProductCode,
                        product.Name,
                        variant.Sku,
                        "BLK",
                        "Black",
                        "M",
                        variant.Price,
                        true),
                    1,
                    0)
            ]);
    }

    private sealed class FakeCustomerRepository(Customer? existingCustomer) : ICustomerRepository
    {
        public List<Customer> AddedCustomers { get; } = [];

        public List<string> CallLog { get; } = [];

        public Task<Customer?> FindByNormalizedPhoneAsync(string normalizedCustomerPhone, CancellationToken cancellationToken = default)
        {
            CallLog.Add("find");
            if (existingCustomer is not null && existingCustomer.NormalizedPhone == normalizedCustomerPhone)
            {
                return Task.FromResult<Customer?>(existingCustomer);
            }

            return Task.FromResult(AddedCustomers.SingleOrDefault(x => x.NormalizedPhone == normalizedCustomerPhone));
        }

        public Task AcquireNormalizedPhoneLockAsync(string normalizedCustomerPhone, CancellationToken cancellationToken = default)
        {
            CallLog.Add("lock");
            return Task.CompletedTask;
        }

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(existingCustomer);

        public Task<IReadOnlyList<Customer>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Customer>>([]);

        public Task AddAsync(Customer aggregate, CancellationToken cancellationToken = default)
        {
            AddedCustomers.Add(aggregate);
            return Task.CompletedTask;
        }

        public void Update(Customer aggregate) { }

        public void Delete(Customer aggregate) { }
    }

    private sealed class FakeOrderRepository(Order? order) : IOrderRepository
    {
        public Task<IReadOnlyCollection<Guid>> FindExpiredCancellableOrderIdsAsync(
            DateTimeOffset orderUpdatedBefore,
            int batchSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Guid>>([]);

        public Task<Order?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(order);
        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(order);
        public Task<IReadOnlyList<Order>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Order>>([]);
        public Task AddAsync(Order aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Order aggregate) { }
        public void Delete(Order aggregate) { }
    }

    private sealed class FakeProductRepository(Product product) : IProductRepository
    {
        public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(product.Sku == sku ? product : null);

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(product.Id == id ? product : null);

        public Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Product>>(ids.Contains(product.Id) ? [product] : []);

        public Task<IReadOnlyList<Product>> GetWithVariantsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Product>>(ids.Contains(product.Id) ? [product] : []);

        public Task<IReadOnlyList<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default)
        {
            var idSet = variantIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<ProductVariant>>(product.Variants.Where(x => idSet.Contains(x.Id)).ToArray());
        }

        // Reference data is rebuilt against whichever id the variant asks for, so a resolved line
        // always finds its colour and size.
        public Task<Color?> GetColorAsync(Guid colorId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Color?>(Color.Create(colorId, "BLK", "Black", "#000000"));

        public Task<Size?> GetSizeAsync(Guid sizeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Size?>(Size.Create(sizeId, "M", "Medium", 40));

        public Task AddAsync(Product aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Product aggregate) { }
        public void Delete(Product aggregate) { }
    }

    private sealed class FakeSalesUnitOfWork : ISalesUnitOfWork
    {
        public int SaveCount { get; private set; }

        public int DiscardCount { get; private set; }

        public Exception? FailOnSave { get; init; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            if (FailOnSave is not null)
            {
                throw FailOnSave;
            }

            return Task.FromResult(1);
        }

        public void DiscardPendingChanges()
        {
            DiscardCount++;
        }
    }

    private sealed class FakeSalesTransactionManager : ISalesTransactionManager
    {
        public int CommitCount { get; private set; }

        public int RollbackCount { get; private set; }

        public Task<ISalesTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ISalesTransaction>(new FakeSalesTransaction(this));
        }

        private sealed class FakeSalesTransaction(FakeSalesTransactionManager owner) : ISalesTransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                owner.CommitCount++;
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                owner.RollbackCount++;
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class FakeCodeGenerator(string code) : ICustomerCodeGenerator, IOrderCodeGenerator
    {
        public Task<string> NextCodeAsync(CancellationToken cancellationToken) => Task.FromResult(code);
    }

    private sealed class NeverClassifyingClassifier : IPersistenceExceptionClassifier
    {
        public PersistenceExceptionClassification? Classify(Exception exception) => null;
    }

    private sealed class AlwaysUniqueViolationClassifier : IPersistenceExceptionClassifier
    {
        public PersistenceExceptionClassification? Classify(Exception exception) =>
            new(ErrorCodes.UniqueViolation, false);
    }
}
