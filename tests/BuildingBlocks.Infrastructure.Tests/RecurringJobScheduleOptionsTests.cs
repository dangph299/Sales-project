namespace BuildingBlocks.Infrastructure.Tests;

public sealed class RecurringJobScheduleOptionsTests
{
    [Fact]
    public void Options_are_valid_when_enabled_and_cron_is_valid()
    {
        var options = new RecurringJobScheduleOptions
        {
            Enabled = true,
            Cron = "*/5 * * * *"
        };

        Assert.True(options.IsValid());
    }

    [Fact]
    public void Options_are_invalid_when_enabled_and_cron_is_empty()
    {
        var options = new RecurringJobScheduleOptions
        {
            Enabled = true,
            Cron = string.Empty
        };

        Assert.False(options.IsValid());
    }

    [Fact]
    public void Options_are_invalid_when_enabled_and_cron_has_wrong_format()
    {
        var options = new RecurringJobScheduleOptions
        {
            Enabled = true,
            Cron = "not a cron"
        };

        Assert.False(options.IsValid());
    }

    [Fact]
    public void Options_are_valid_when_disabled_even_without_cron()
    {
        var options = new RecurringJobScheduleOptions
        {
            Enabled = false,
            Cron = string.Empty
        };

        Assert.True(options.IsValid());
    }
}
