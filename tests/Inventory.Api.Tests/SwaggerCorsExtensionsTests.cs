using Inventory.Api.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Inventory.Api.Tests;

public sealed class SwaggerCorsExtensionsTests
{
    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Inventory.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Fact]
    public void AddSwaggerCors_in_development_registers_a_policy_allowing_the_sales_origins()
    {
        var services = new ServiceCollection();

        services.AddSwaggerCors(new FakeHostEnvironment(Environments.Development));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = options.GetPolicy(SwaggerCorsExtensions.PolicyName);

        Assert.NotNull(policy);
        Assert.Contains("http://localhost:5000", policy!.Origins);
        Assert.Contains("https://localhost:5002", policy.Origins);
        Assert.True(policy.AllowAnyHeader);
        Assert.True(policy.AllowAnyMethod);
        Assert.False(policy.SupportsCredentials);
    }

    [Fact]
    public void AddSwaggerCors_outside_development_registers_no_policy()
    {
        var services = new ServiceCollection();

        services.AddSwaggerCors(new FakeHostEnvironment(Environments.Production));

        var provider = services.BuildServiceProvider();
        var corsOptions = provider.GetService<IOptions<CorsOptions>>();
        var policy = corsOptions?.Value.GetPolicy(SwaggerCorsExtensions.PolicyName);

        Assert.Null(policy);
    }
}
