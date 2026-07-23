using BuildingBlocks.Contracts;
using Dashboard.Bff.Auth;
using Dashboard.Bff.Clients;
using Dashboard.Bff.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dashboard.Bff.Tests;

/// <summary>
/// Verifies that <see cref="DashboardBffServiceCollectionExtensions.AddDashboardBff"/> registers a
/// fully resolvable DI container for the downstream HTTP clients and their auth dependencies.
/// </summary>
public sealed class DashboardBffServiceCollectionExtensionsTests
{
    private static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "sales-api",
            ["Jwt:Audience"] = "sales-clients",
            ["Jwt:Key"] = "local-development-key-change-before-production",
            ["Downstream:SalesBaseUrl"] = "http://localhost:5000",
            ["Downstream:InventoryBaseUrl"] = "http://localhost:5001",
            ["ServiceAccount:UserName"] = "",
            ["ServiceAccount:Password"] = "",
            ["ServiceAccount:AllowAdminDevFallback"] = "true",
            ["Dashboard:Cache:Key"] = "dashboard:snapshot",
            ["Dashboard:Cache:TtlSeconds"] = "300",
            ["Dashboard:RefreshJob:Enabled"] = "false",
            ["Dashboard:RefreshJob:Cron"] = "* * * * *",
            ["Dashboard:RefreshJob:Queue"] = "default",
        });
        builder.Environment.EnvironmentName = Environments.Development;

        builder.AddDashboardBff();

        return builder;
    }

    [Fact]
    public void AddDashboardBff_registers_a_resolvable_ISalesClient()
    {
        using var provider = CreateBuilder().Services.BuildServiceProvider();

        var client = provider.GetRequiredService<ISalesClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddDashboardBff_registers_a_resolvable_IInventoryClient()
    {
        using var provider = CreateBuilder().Services.BuildServiceProvider();

        var client = provider.GetRequiredService<IInventoryClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddDashboardBff_registers_a_singleton_IServiceTokenProvider()
    {
        using var provider = CreateBuilder().Services.BuildServiceProvider();

        var first = provider.GetRequiredService<IServiceTokenProvider>();
        var second = provider.GetRequiredService<IServiceTokenProvider>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddDashboardBff_registers_the_shared_error_message_provider()
    {
        using var provider = CreateBuilder().Services.BuildServiceProvider();

        var messageProvider = provider.GetRequiredService<IErrorMessageProvider>();

        Assert.IsType<DefaultErrorMessageProvider>(messageProvider);
    }

    [Fact]
    public void AddDashboardBff_registers_a_valid_service_provider()
    {
        using var provider = CreateBuilder().Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Assert.NotNull(provider);
    }
}
