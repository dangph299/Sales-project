namespace BuildingBlocks.Infrastructure.Tests;

public sealed class RecurringJobSettingsTests
{
    [Fact]
    public void Settings_are_valid_when_enabled_with_queue_and_valid_cron()
    {
        var settings = new RecurringJobSettings
        {
            Enabled = true,
            Queue = "critical",
            Cron = "*/5 * * * *"
        };

        Assert.True(settings.IsValid());
    }

    [Fact]
    public void Settings_are_invalid_when_enabled_and_cron_is_empty()
    {
        var settings = new RecurringJobSettings
        {
            Enabled = true,
            Queue = "critical",
            Cron = string.Empty
        };

        Assert.False(settings.IsValid());
    }

    [Fact]
    public void Settings_are_invalid_when_enabled_and_cron_has_wrong_format()
    {
        var settings = new RecurringJobSettings
        {
            Enabled = true,
            Queue = "critical",
            Cron = "not a cron"
        };

        Assert.False(settings.IsValid());
    }

    [Fact]
    public void Settings_are_invalid_when_enabled_and_queue_is_empty()
    {
        var settings = new RecurringJobSettings
        {
            Enabled = true,
            Queue = "   ",
            Cron = "*/5 * * * *"
        };

        Assert.False(settings.IsValid());
    }

    [Fact]
    public void Settings_are_valid_when_disabled_even_without_schedule()
    {
        var settings = new RecurringJobSettings
        {
            Enabled = false,
            Cron = string.Empty
        };

        Assert.True(settings.IsValid());
    }

    [Fact]
    public void Settings_are_invalid_when_enabled_and_queue_is_not_configured()
    {
        var settings = new RecurringJobSettings
        {
            Enabled = true,
            Cron = "*/5 * * * *"
        };

        Assert.False(settings.IsValid());
    }
}
