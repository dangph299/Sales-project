using Dashboard.Bff.Options;

namespace Dashboard.Bff.Tests;

/// <summary>
/// Exercises the env-aware validation predicates applied to <see cref="ServiceAccountOptions"/>
/// in <c>DashboardBffServiceCollectionExtensions</c>. The predicates are duplicated here (rather
/// than invoked through the DI pipeline) because they are simple lambdas registered inline against
/// <c>OptionsBuilder</c>, which is not otherwise unit-testable in isolation.
/// </summary>
public sealed class ServiceAccountOptionsValidationTests
{
    private static bool NonDevelopmentIsValid(ServiceAccountOptions o) =>
        !string.IsNullOrWhiteSpace(o.UserName) && !string.IsNullOrWhiteSpace(o.Password);

    private static bool DevelopmentIsValid(ServiceAccountOptions o) =>
        o.AllowAdminDevFallback || (!string.IsNullOrWhiteSpace(o.UserName) && !string.IsNullOrWhiteSpace(o.Password));

    [Fact]
    public void NonDevelopment_rejects_blank_credentials()
    {
        var options = new ServiceAccountOptions { UserName = "", Password = "" };

        Assert.False(NonDevelopmentIsValid(options));
    }

    [Fact]
    public void NonDevelopment_rejects_blank_credentials_even_with_dev_fallback_flag()
    {
        var options = new ServiceAccountOptions { UserName = "", Password = "", AllowAdminDevFallback = true };

        Assert.False(NonDevelopmentIsValid(options));
    }

    [Fact]
    public void NonDevelopment_accepts_populated_credentials()
    {
        var options = new ServiceAccountOptions { UserName = "svc", Password = "secret" };

        Assert.True(NonDevelopmentIsValid(options));
    }

    [Fact]
    public void Development_rejects_blank_credentials_without_dev_fallback()
    {
        var options = new ServiceAccountOptions { UserName = "", Password = "", AllowAdminDevFallback = false };

        Assert.False(DevelopmentIsValid(options));
    }

    [Fact]
    public void Development_accepts_blank_credentials_with_dev_fallback()
    {
        var options = new ServiceAccountOptions { UserName = "", Password = "", AllowAdminDevFallback = true };

        Assert.True(DevelopmentIsValid(options));
    }
}
