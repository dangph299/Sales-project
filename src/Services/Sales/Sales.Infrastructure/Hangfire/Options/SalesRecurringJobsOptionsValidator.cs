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

        ValidateMessagingJob(
            failures,
            nameof(SalesRecurringJobsOptions.ReplayDeadLetter),
            options.ReplayDeadLetter.IsValid(),
            "a non-empty Schedule:Queue, a valid Schedule:Cron, positive BatchSize, and non-negative RetryDelaySeconds");
        ValidateMessagingJob(
            failures,
            nameof(SalesRecurringJobsOptions.KafkaLagMonitor),
            options.KafkaLagMonitor.IsValid(),
            "a non-empty Schedule:Queue, a valid Schedule:Cron, GroupId, at least one Topic, non-negative WarningThreshold, and positive RequestTimeoutSeconds");
        ValidateMessagingJob(
            failures,
            nameof(SalesRecurringJobsOptions.InboxCleanup),
            options.InboxCleanup.IsValid(),
            "a non-empty Schedule:Queue, a valid Schedule:Cron, positive BatchSize, and positive RetentionDays");
        ValidateMessagingJob(
            failures,
            nameof(SalesRecurringJobsOptions.FailedOutboxRetry),
            options.FailedOutboxRetry.IsValid(),
            "a non-empty Schedule:Queue, a valid Schedule:Cron, positive BatchSize, and non-negative RetryDelaySeconds");
        ValidateMessagingJob(
            failures,
            nameof(SalesRecurringJobsOptions.OutboxPendingMonitor),
            options.OutboxPendingMonitor.IsValid(),
            "a non-empty Schedule:Queue, a valid Schedule:Cron, non-negative BacklogWarningThreshold, and non-negative OldestPendingWarningSeconds");

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }

    private static void ValidateMessagingJob(
        List<string> failures,
        string jobName,
        bool isValid,
        string expectation)
    {
        if (isValid)
        {
            return;
        }

        failures.Add(
            $"'{SalesRecurringJobsOptions.SectionName}:{jobName}' is invalid: an enabled job needs {expectation}.");
    }
}
