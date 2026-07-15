using Cronos;

namespace BuildingBlocks.Infrastructure;

public class RecurringJobScheduleOptions
{
    public bool Enabled { get; init; } = true;

    public string Cron { get; init; } = string.Empty;

    public virtual bool IsValid()
    {
        return !Enabled || IsValidCron(Cron);
    }

    protected static bool IsValidCron(string cron)
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
