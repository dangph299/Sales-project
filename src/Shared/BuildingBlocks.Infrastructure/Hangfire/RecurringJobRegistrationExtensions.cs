using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure;

public static class RecurringJobRegistrationExtensions
{
    public static void RegisterRecurringJobs(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var serviceScope = serviceProvider.CreateScope();

        var recurringJobRegistrations = serviceScope.ServiceProvider
            .GetServices<IRecurringJobRegistration>();

        foreach (var recurringJobRegistration in recurringJobRegistrations)
        {
            recurringJobRegistration.Register();
        }
    }
}
