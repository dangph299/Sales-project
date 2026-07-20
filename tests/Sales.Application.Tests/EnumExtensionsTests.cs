using System.ComponentModel;
using BuildingBlocks.Application;

namespace Sales.Application.Tests;

/// <summary>
/// Covers the shared enum display-text helper: attribute precedence, the fallback that keeps
/// <see cref="DescriptionAttribute"/> optional, caching, and thread safety.
/// </summary>
public sealed class EnumExtensionsTests
{
    [Fact]
    public void Declared_description_is_used_when_the_member_has_one()
    {
        Assert.Equal("Stock reserved", DescribedOutcome.Reserved.GetDescription());
    }

    [Fact]
    public void Member_name_is_used_when_the_member_declares_no_description()
    {
        Assert.Equal(nameof(DescribedOutcome.Ignored), DescribedOutcome.Ignored.GetDescription());
    }

    [Fact]
    public void Enum_without_any_description_falls_back_to_ToString_for_every_member()
    {
        // Pins the promise that an enum need not declare descriptions at all - this is the shape
        // OrderTransition in Sales.Infrastructure relies on.
        foreach (var outcome in Enum.GetValues<PlainOutcome>())
        {
            Assert.Equal(outcome.ToString(), outcome.GetDescription());
        }
    }

    [Fact]
    public void Blank_description_falls_back_to_the_member_name()
    {
        Assert.Equal(nameof(DescribedOutcome.Blank), DescribedOutcome.Blank.GetDescription());
    }

    [Fact]
    public void Undefined_value_falls_back_to_ToString_instead_of_throwing()
    {
        var undefinedOutcome = (PlainOutcome)999;

        Assert.Equal(undefinedOutcome.ToString(), undefinedOutcome.GetDescription());
    }

    [Fact]
    public void Repeated_calls_return_a_stable_description()
    {
        // Note: this pins the observable contract only. That reflection runs once is a property of the
        // static readonly cache initializer, which no behavioral assertion can distinguish - reference
        // equality would hold anyway, because Enum.ToString() returns the runtime's cached name and
        // attribute string literals are interned.
        var descriptions = Enumerable
            .Range(0, 5)
            .Select(_ => DescribedOutcome.Reserved.GetDescription())
            .ToArray();

        Assert.All(descriptions, description => Assert.Equal("Stock reserved", description));
    }

    [Fact]
    public void Two_enums_do_not_share_a_cache()
    {
        Assert.Equal("Stock reserved", DescribedOutcome.Reserved.GetDescription());
        Assert.Equal(nameof(PlainOutcome.Reserved), PlainOutcome.Reserved.GetDescription());
    }

    [Fact]
    public void Concurrent_first_use_resolves_one_consistent_description()
    {
        var descriptions = new string[64];

        Parallel.For(0, descriptions.Length, index =>
        {
            descriptions[index] = ConcurrentOutcome.Released.GetDescription();
        });

        Assert.All(descriptions, description => Assert.Equal("Stock released", description));
    }

    private enum PlainOutcome
    {
        Ignored,
        Reserved,
        Released
    }

    private enum DescribedOutcome
    {
        Ignored,

        [Description("Stock reserved")]
        Reserved,

        [Description("   ")]
        Blank
    }

    /// <summary>
    /// Used by the concurrency test only, so its cache is guaranteed to be cold when that test runs.
    /// </summary>
    private enum ConcurrentOutcome
    {
        [Description("Stock released")]
        Released
    }
}
