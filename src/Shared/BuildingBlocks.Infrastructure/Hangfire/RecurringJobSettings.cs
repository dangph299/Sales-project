using Cronos;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Schedule settings shared by recurring jobs. Job identifiers are deliberately absent: they stay in
/// service-owned constants so a configuration change cannot create a second recurring job.
/// </summary>
public sealed class RecurringJobSettings
{
    public bool Enabled { get; init; } = true;

    public string Cron { get; init; } = string.Empty;

    /// <summary>
    /// Deliberately has no default: an enabled job must name its queue, so a configuration file that
    /// omits it fails startup instead of silently moving the job to another queue.
    /// </summary>
    public string Queue { get; init; } = string.Empty;

    /// <summary>
    /// A disabled job needs no schedule; an enabled job needs a queue and a parsable cron expression.
    /// </summary>
    public bool IsValid()
    {
        if (!Enabled)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(Queue)
            && IsValidCron(Cron);
    }

    private static bool IsValidCron(string cron)
    {
        if (string.IsNullOrWhiteSpace(cron))
        {
            return false;
        }

        var fieldCount = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var format = fieldCount == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard;

        return CronExpression.TryParse(cron, format, out _);
    }
}
