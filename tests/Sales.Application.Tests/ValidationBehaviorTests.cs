using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application.Features.Customers.Commands;
using Sales.Application.Features.Customers.Interfaces;
using Sales.Application.Features.Orders.Commands;
using Sales.Application.Features.Orders.DTOs;
using Sales.Domain;

namespace Sales.Application.Tests;

public sealed class ValidationBehaviorTests
{
    private static IMediator BuildMediator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateCustomer>());
        services.AddSalesApplication();
        services.AddSingleton<IRepository<Customer>, FakeCustomerRepository>();
        services.AddSingleton<ICustomerCodeGenerator, FakeCustomerCodeGenerator>();
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Invalid_command_is_rejected_before_reaching_the_handler()
    {
        var mediator = BuildMediator();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => mediator.Send(new CreateCustomer("", "123")));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateCustomer.Name));
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateCustomer.Phone));
    }

    [Fact]
    public async Task Valid_command_reaches_the_handler()
    {
        var mediator = BuildMediator();

        var dto = await mediator.Send(new CreateCustomer("Nguyen Van A", "0912345678"));

        Assert.Equal("Nguyen Van A", dto.Name);
    }

    [Fact]
    public async Task Missing_order_line_discount_is_rejected()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateOrder>());
        services.AddSalesApplication();
        services.AddSingleton<IOrderRepository, FakeOrderRepository>();
        services.AddSingleton<IRepository<Customer>, FakeCustomerRepository>();
        services.AddSingleton<ICustomerCodeGenerator, FakeCustomerCodeGenerator>();
        services.AddSingleton<IProductRepository, FakeProductRepository>();
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            mediator.Send(new CreateOrder(Guid.NewGuid(), [new OrderLineInput(Guid.NewGuid(), 1, null)])));

        Assert.Contains(ex.Errors, e => e.PropertyName.EndsWith(nameof(OrderLineInput.DiscountPercent), StringComparison.Ordinal));
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public Task<IReadOnlyCollection<Guid>> FindExpiredCancellableOrderIdsAsync(
            DateTimeOffset orderUpdatedBefore,
            int batchSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Guid>>([]);

        public Task<Order?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);
        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);
        public Task<IReadOnlyList<Order>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Order>>([]);
        public Task AddAsync(Order aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Order aggregate) { }
        public void Delete(Order aggregate) { }
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) => Task.FromResult<Product?>(null);
        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Product?>(null);
        public Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Product>>([]);
        public Task AddAsync(Product aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Product aggregate) { }
        public void Delete(Product aggregate) { }
    }

    private sealed class FakeCustomerRepository : IRepository<Customer>
    {
        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Customer?>(null);
        public Task<IReadOnlyList<Customer>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Customer>>([]);
        public Task AddAsync(Customer aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Customer aggregate) { }
        public void Delete(Customer aggregate) { }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeCustomerCodeGenerator : ICustomerCodeGenerator
    {
        public Task<string> NextCodeAsync(CancellationToken cancellationToken = default) => Task.FromResult("CUS000001");
    }
}
