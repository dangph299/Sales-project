using NetArchTest.Rules;

namespace Sales.Architecture.Tests;

public sealed class DependencyRulesTests
{
    [Fact]
    public void Sales_domain_does_not_depend_on_outer_layers_or_other_services()
    {
        var result = Types.InAssembly(typeof(Sales.Domain.Product).Assembly).ShouldNot()
            .HaveDependencyOnAny(
                "Sales.Application",
                "Sales.Infrastructure",
                "Inventory",
                "AuditLog",
                "BuildingBlocks.Application",
                "BuildingBlocks.Infrastructure",
                "BuildingBlocks.Web",
                "Microsoft.EntityFrameworkCore",
                "KafkaFlow").GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Sales_application_does_not_depend_on_infrastructure_or_other_services()
    {
        var result = Types.InAssembly(typeof(Sales.Application.CreateProduct).Assembly).ShouldNot()
            .HaveDependencyOnAny(
                "Sales.Infrastructure",
                "Inventory",
                "AuditLog",
                "Microsoft.EntityFrameworkCore",
                "BuildingBlocks.Infrastructure",
                "BuildingBlocks.Web",
                "KafkaFlow").GetResult();
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
            .HaveDependencyOnAny(
                "Inventory.Application",
                "Inventory.Infrastructure",
                "Sales",
                "AuditLog",
                "BuildingBlocks.Application",
                "BuildingBlocks.Infrastructure",
                "BuildingBlocks.Web",
                "Microsoft.EntityFrameworkCore",
                "KafkaFlow").GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void BuildingBlocks_domain_is_framework_independent()
    {
        var result = Types.InAssembly(typeof(BuildingBlocks.Domain.AggregateRoot<>).Assembly).ShouldNot()
            .HaveDependencyOnAny(
                "BuildingBlocks.Application",
                "BuildingBlocks.Infrastructure",
                "BuildingBlocks.Web",
                "Sales",
                "Inventory",
                "AuditLog",
                "Microsoft.EntityFrameworkCore",
                "MediatR",
                "KafkaFlow",
                "Serilog",
                "Microsoft.AspNetCore").GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void BuildingBlocks_application_does_not_depend_on_infrastructure_or_web()
    {
        var result = Types.InAssembly(typeof(BuildingBlocks.Application.IUnitOfWork).Assembly).ShouldNot()
            .HaveDependencyOnAny(
                "BuildingBlocks.Infrastructure",
                "BuildingBlocks.Web",
                "Sales",
                "Inventory",
                "AuditLog",
                "Microsoft.EntityFrameworkCore",
                "KafkaFlow",
                "Microsoft.AspNetCore").GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void BuildingBlocks_contracts_are_transport_and_framework_independent()
    {
        var result = Types.InAssembly(typeof(BuildingBlocks.Contracts.EventEnvelope).Assembly).ShouldNot()
            .HaveDependencyOnAny(
                "BuildingBlocks.Domain",
                "BuildingBlocks.Application",
                "BuildingBlocks.Infrastructure",
                "BuildingBlocks.Web",
                "Sales",
                "Inventory",
                "AuditLog",
                "Microsoft.EntityFrameworkCore",
                "KafkaFlow",
                "Microsoft.AspNetCore").GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void BuildingBlocks_contracts_do_not_contain_trace_parsing_behavior()
    {
        var traceParserTypes = Types.InAssembly(typeof(BuildingBlocks.Contracts.EventEnvelope).Assembly)
            .That()
            .HaveName("TraceContextParser")
            .GetTypes();

        Assert.Empty(traceParserTypes);
    }

    [Fact]
    public void BuildingBlocks_infrastructure_does_not_depend_on_services()
    {
        var result = Types.InAssembly(typeof(BuildingBlocks.Infrastructure.OutboxMessage).Assembly).ShouldNot()
            .HaveDependencyOnAny(
                "Sales",
                "Inventory",
                "AuditLog",
                "BuildingBlocks.Web",
                "Microsoft.AspNetCore").GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void BuildingBlocks_web_does_not_depend_on_services_or_infrastructure()
    {
        var result = Types.InAssembly(typeof(BuildingBlocks.Web.RequestObservabilityMiddleware).Assembly).ShouldNot()
            .HaveDependencyOnAny(
                "Sales",
                "Inventory",
                "AuditLog",
                "BuildingBlocks.Domain",
                "BuildingBlocks.Infrastructure",
                "KafkaFlow",
                "Hangfire").GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void BuildingBlocks_observability_does_not_depend_on_services_or_web_hosts()
    {
        var result = Types.InAssembly(typeof(BuildingBlocks.Observability.OutboxMetrics).Assembly).ShouldNot()
            .HaveDependencyOnAny(
                "Sales",
                "Inventory",
                "AuditLog",
                "BuildingBlocks.Domain",
                "BuildingBlocks.Application",
                "BuildingBlocks.Infrastructure",
                "BuildingBlocks.Web",
                "Microsoft.AspNetCore",
                "KafkaFlow").GetResult();
        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
