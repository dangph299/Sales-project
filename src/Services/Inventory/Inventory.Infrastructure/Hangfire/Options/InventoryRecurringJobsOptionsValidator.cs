using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure;

/// <summary>
/// Fails startup with a message naming the offending Inventory recurring job.
/// </summary>
public sealed class InventoryRecurringJobsOptionsValidator : IValidateOptions<InventoryRecurringJobsOptions>
{
    public ValidateOptionsResult Validate(string? name, InventoryRecurringJobsOptions options)
    {
        var failures = new List<string>();

        ValidateJob(
            failures,
            nameof(InventoryRecurringJobsOptions.ReplayDeadLetter),
            options.ReplayDeadLetter.IsValid(),
            "a non-empty Schedule:Queue, a valid Schedule:Cron, positive BatchSize, and non-negative RetryDelaySeconds");
        ValidateJob(
            failures,
            nameof(InventoryRecurringJobsOptions.KafkaLagMonitor),
            options.KafkaLagMonitor.IsValid(),
            "a non-empty Schedule:Queue, a valid Schedule:Cron, GroupId, at least one Topic, non-negative WarningThreshold, and positive RequestTimeoutSeconds");
        ValidateJob(
            failures,
            nameof(InventoryRecurringJobsOptions.InboxCleanup),
            options.InboxCleanup.IsValid(),
            "a non-empty Schedule:Queue, a valid Schedule:Cron, positive BatchSize, and positive RetentionDays");
        ValidateJob(
            failures,
            nameof(InventoryRecurringJobsOptions.FailedOutboxRetry),
            options.FailedOutboxRetry.IsValid(),
            "a non-empty Schedule:Queue, a valid Schedule:Cron, positive BatchSize, and non-negative RetryDelaySeconds");
        ValidateJob(
            failures,
            nameof(InventoryRecurringJobsOptions.OutboxPendingMonitor),
            options.OutboxPendingMonitor.IsValid(),
            "a non-empty Schedule:Queue, a valid Schedule:Cron, non-negative BacklogWarningThreshold, and non-negative OldestPendingWarningSeconds");

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }

    private static void ValidateJob(
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
            $"'{InventoryRecurringJobsOptions.SectionName}:{jobName}' is invalid: an enabled job needs {expectation}.");
    }
}
