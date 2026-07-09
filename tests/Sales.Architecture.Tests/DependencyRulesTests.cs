using NetArchTest.Rules;

namespace Sales.Architecture.Tests;

public sealed class DependencyRulesTests
{
    [Fact]
    public void Sales_domain_does_not_depend_on_outer_layers_or_other_services()
    {
        var result = Types.InAssembly(typeof(Sales.Domain.Product).Assembly).ShouldNot()
            .HaveDependencyOnAny("Sales.Application", "Sales.Infrastructure", "Inventory", "AuditLog").GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Sales_application_does_not_depend_on_infrastructure_or_other_services()
    {
        var result = Types.InAssembly(typeof(Sales.Application.CreateProduct).Assembly).ShouldNot()
            .HaveDependencyOnAny("Sales.Infrastructure", "Inventory", "AuditLog", "Microsoft.EntityFrameworkCore", "BuildingBlocks", "KafkaFlow").GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_repositories_do_not_expose_queryables()
    {
        var repositoryTypes = new[]
        {
            typeof(Sales.Domain.IProductRepository),
            typeof(Sales.Domain.IRepository<Sales.Domain.Customer>),
            typeof(Sales.Domain.IOrderRepository)
        };

        var exposesQueryable = repositoryTypes.SelectMany(x => x.GetProperties())
            .Any(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(IQueryable<>));
        Assert.False(exposesQueryable);
    }

    [Fact]
    public void Inventory_domain_is_isolated()
    {
        var result = Types.InAssembly(typeof(Inventory.Domain.InventoryItem).Assembly).ShouldNot()
            .HaveDependencyOnAny("Inventory.Application", "Inventory.Infrastructure", "Sales", "AuditLog").GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
