namespace BuildingBlocks.Infrastructure;

public class RecurringJobScheduleOptions
{
    public bool Enabled { get; init; } = true;

    public string Cron { get; init; } = string.Empty;

    public virtual bool IsValid()
    {
        if (!Enabled)
        {
            return true;
        }

        return IsValidCron(Cron);
    }

    protected static bool IsValidCron(string cron)
    {
        if (string.IsNullOrWhiteSpace(cron))
        {
            return false;
        }

        var fieldCount = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (fieldCount is not (5 or 6))
        {
            return false;
        }

        var fields = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstFieldIndex = fieldCount == 6 ? 1 : 0;

        return IsValidCronField(fields[firstFieldIndex], 0, 59)
            && IsValidCronField(fields[firstFieldIndex + 1], 0, 23)
            && IsValidCronField(fields[firstFieldIndex + 2], 1, 31)
            && IsValidCronField(fields[firstFieldIndex + 3], 1, 12)
            && IsValidCronField(fields[firstFieldIndex + 4], 0, 7);
    }

    private static bool IsValidCronField(string field, int minValue, int maxValue)
    {
        if (field is "*" or "?")
        {
            return true;
        }

        var fieldParts = field.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return fieldParts.Length > 0
            && fieldParts.All(fieldPart => IsValidCronFieldPart(fieldPart, minValue, maxValue));
    }

    private static bool IsValidCronFieldPart(string fieldPart, int minValue, int maxValue)
    {
        var rangeAndStep = fieldPart.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (rangeAndStep.Length > 2)
        {
            return false;
        }

        if (rangeAndStep.Length == 2 && !IsValidCronNumber(rangeAndStep[1], 1, maxValue))
        {
            return false;
        }

        var range = rangeAndStep[0];
        if (range == "*")
        {
            return true;
        }

        var rangeParts = range.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (rangeParts.Length == 1)
        {
            return IsValidCronNumber(rangeParts[0], minValue, maxValue);
        }

        if (rangeParts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(rangeParts[0], out var start) ||
            !int.TryParse(rangeParts[1], out var end))
        {
            return false;
        }

        return start >= minValue
            && end <= maxValue
            && start <= end;
    }

    private static bool IsValidCronNumber(string value, int minValue, int maxValue)
    {
        return int.TryParse(value, out var number)
            && number >= minValue
            && number <= maxValue;
    }
}
