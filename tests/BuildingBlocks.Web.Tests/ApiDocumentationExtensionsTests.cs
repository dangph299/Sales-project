using BuildingBlocks.Web.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Web.Tests;

public sealed class ApiDocumentationExtensionsTests
{
    private static async Task<(WebApplication App, HttpClient Client)> StartAppAsync(
        IReadOnlyCollection<SwaggerDocumentEndpoint>? additionalDocuments)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddApiDocumentation("Sales API", "Sales service API.");

        var app = builder.Build();
        app.UseApiDocumentation("Sales API", additionalDocuments: additionalDocuments);

        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    [Fact]
    public async Task UseApiDocumentation_without_additional_documents_serves_only_the_local_document()
    {
        var (app, client) = await StartAppAsync(additionalDocuments: null);
        await using var _ = app;

        var body = await client.GetStringAsync("/swagger/index.js");

        Assert.Contains("/swagger/v1/swagger.json", body);
        Assert.Contains("Sales API v1", body);
    }

    [Fact]
    public async Task UseApiDocumentation_with_additional_documents_lists_every_document()
    {
        var additionalDocuments = new[]
        {
            new SwaggerDocumentEndpoint("Inventory API", "http://localhost:5001/swagger/v1/swagger.json")
        };

        var (app, client) = await StartAppAsync(additionalDocuments);
        await using var _ = app;

        var body = await client.GetStringAsync("/swagger/index.js");

        Assert.Contains("/swagger/v1/swagger.json", body);
        Assert.Contains("Sales API v1", body);
        Assert.Contains("http://localhost:5001/swagger/v1/swagger.json", body);
        Assert.Contains("Inventory API", body);
    }
}
