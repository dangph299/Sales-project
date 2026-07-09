using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application;
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
}
