using BuildingBlocks.Application;
using MediatR;

namespace Sales.Architecture.Tests;

public sealed class CqrsHandlerConventionTests
{
    [Theory]
    [MemberData(nameof(ApplicationAssemblies))]
    public void Request_handlers_implement_a_shared_command_or_query_handler_marker(System.Reflection.Assembly assembly)
    {
        var openRequestHandler = typeof(IRequestHandler<>);
        var openRequestHandlerOfT = typeof(IRequestHandler<,>);
        var openCommandHandler = typeof(ICommandHandler<>);
        var openCommandHandlerOfT = typeof(ICommandHandler<,>);
        var openQueryHandler = typeof(IQueryHandler<,>);

        var violations = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == openRequestHandler ||
                 i.GetGenericTypeDefinition() == openRequestHandlerOfT)))
            .Where(type => !type.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == openCommandHandler ||
                 i.GetGenericTypeDefinition() == openCommandHandlerOfT ||
                 i.GetGenericTypeDefinition() == openQueryHandler)))
            .Select(type => type.FullName)
            .ToArray();

        Assert.True(violations.Length == 0,
            $"Handlers must implement ICommandHandler/IQueryHandler instead of MediatR's IRequestHandler directly: {string.Join(", ", violations)}");
    }

    public static TheoryData<System.Reflection.Assembly> ApplicationAssemblies() =>
    [
        typeof(Sales.Application.DependencyInjection).Assembly,
        typeof(Inventory.Application.DependencyInjection).Assembly
    ];
}
