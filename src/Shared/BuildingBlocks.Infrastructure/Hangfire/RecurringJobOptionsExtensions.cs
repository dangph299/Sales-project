using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure;

public static class RecurringJobOptionsExtensions
{
    public static OptionsBuilder<TOptions> AddRecurringJobOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath,
        string validationMessage)
        where TOptions : RecurringJobScheduleOptions, new()
    {
        var optionsBuilder = services.AddOptions<TOptions>();
        optionsBuilder.Configure(recurringJobOptions => configuration.GetSection(sectionPath).Bind(recurringJobOptions));

        return optionsBuilder
            .Validate(recurringJobOptions => recurringJobOptions.IsValid(), validationMessage)
            .ValidateOnStart();
    }

    public static OptionsBuilder<TOptions> AddRecurringJobOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string optionsName,
        string sectionPath,
        string validationMessage)
        where TOptions : RecurringJobScheduleOptions, new()
    {
        var optionsBuilder = services.AddOptions<TOptions>(optionsName);
        optionsBuilder.Configure(recurringJobOptions => configuration.GetSection(sectionPath).Bind(recurringJobOptions));

        return optionsBuilder
            .Validate(recurringJobOptions => recurringJobOptions.IsValid(), validationMessage)
            .ValidateOnStart();
    }
}
