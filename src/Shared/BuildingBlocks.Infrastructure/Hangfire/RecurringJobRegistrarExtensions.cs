using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

public static class RecurringJobRegistrarExtensions
{
    public static void RegisterRecurringJobs(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var serviceScope = serviceProvider.CreateScope();

        var recurringJobDefinitions = serviceScope.ServiceProvider
            .GetServices<IRecurringJobDefinition>();

        foreach (var recurringJobDefinition in recurringJobDefinitions)
        {
            recurringJobDefinition.Register();
        }
    }
}
