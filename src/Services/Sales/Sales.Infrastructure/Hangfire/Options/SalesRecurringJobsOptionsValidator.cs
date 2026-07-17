using Microsoft.Extensions.Options;

namespace Sales.Infrastructure;

/// <summary>
/// Fails startup with a message naming the offending job rather than a single generic error.
/// </summary>
public sealed class SalesRecurringJobsOptionsValidator : IValidateOptions<SalesRecurringJobsOptions>
{
    public ValidateOptionsResult Validate(string? name, SalesRecurringJobsOptions options)
    {
        var failures = new List<string>();

        if (!options.MaintenanceCleanup.IsValid())
        {
            failures.Add(
                $"'{SalesRecurringJobsOptions.SectionName}:{nameof(SalesRecurringJobsOptions.MaintenanceCleanup)}' "
                + "is invalid: an enabled job needs a non-empty Queue and a valid Cron expression.");
        }

        if (!options.CancelExpiredPendingOrders.IsValid())
        {
            failures.Add(
                $"'{SalesRecurringJobsOptions.SectionName}:{nameof(SalesRecurringJobsOptions.CancelExpiredPendingOrders)}' "
                + "is invalid: an enabled job needs a non-empty Schedule:Queue, a valid Schedule:Cron, "
                + "and positive ExpirationMinutes and BatchSize.");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}
