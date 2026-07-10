# Aggregated Swagger UI (Sales + Inventory) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `http://localhost:5000/swagger` (Sales.Api) show a document-picker dropdown containing both "Sales API" and "Inventory API", where the Inventory OpenAPI document is fetched directly by the browser from Inventory.Api via CORS — no gateway, proxy, or new service.

**Architecture:** `BuildingBlocks.Web.OpenApi.ApiDocumentationExtensions.UseApiDocumentation` gains an `additionalDocuments` parameter (typed `SwaggerDocumentEndpoint`, not tuples) that registers extra `SwaggerEndpoint` entries in Swashbuckle's UI options. Sales.Api reads the Inventory Swagger URL from configuration and passes it in. Inventory.Api adds a Development-only CORS policy so the browser can fetch its `swagger.json` and execute "Try it out" calls from the page served at `http://localhost:5000`.

**Tech Stack:** ASP.NET Core 10 minimal hosting, Swashbuckle.AspNetCore 7.3.0, xunit 2.9.3, Microsoft.AspNetCore.TestHost.

## Global Constraints

- Development-only: both the extra Swagger document registration and the Inventory CORS policy must be no-ops outside `Development` (matches the existing `UseApiDocumentation` gate).
- No API Gateway, reverse proxy, YARP, new service, or HTTP proxy endpoint inside Sales.Api — the browser fetches Inventory's `swagger.json` directly.
- `BuildingBlocks.Web` must not reference `Sales.Api` or `Inventory.Api`, and must not read `IConfiguration` or know any application-specific URL.
- `Inventory.Api` must not reference `Sales.Api`.
- Do not modify: `AddApiDocumentation()`, JWT configuration, `AuthorizeOperationFilter`, `SwaggerGen` configuration, API routing, `docker-compose.yml` ports, or Inventory's standalone Swagger UI at `http://localhost:5001/swagger`.
- Public APIs use the `SwaggerDocumentEndpoint` record — never `(string Name, string Url)` tuples.
- Inventory CORS policy allows exactly `http://localhost:5000` and `https://localhost:5002`, any header, any method. Do not call `AllowCredentials()`.
- Inventory.Api middleware order must be: `UseRouting()` → `UseCors()` → `UseAuthentication()` → `UseAuthorization()`.
- Existing Swagger behavior must be unchanged when `additionalDocuments` is omitted (backward compatible).

---

### Task 1: `SwaggerDocumentEndpoint` model + multi-document `UseApiDocumentation`

**Files:**
- Create: `src/Shared/BuildingBlocks.Web/OpenApi/SwaggerDocumentEndpoint.cs`
- Modify: `src/Shared/BuildingBlocks.Web/OpenApi/ApiDocumentationExtensions.cs:82-97` (the `UseApiDocumentation` method)
- Create: `tests/BuildingBlocks.Web.Tests/BuildingBlocks.Web.Tests.csproj`
- Create: `tests/BuildingBlocks.Web.Tests/ApiDocumentationExtensionsTests.cs`
- Modify: `Sales.sln` (register the new test project)

**Interfaces:**
- Produces: `BuildingBlocks.Web.OpenApi.SwaggerDocumentEndpoint(string DisplayName, string Url)` — a `sealed record`, used by Task 2.
- Produces: `WebApplication UseApiDocumentation(this WebApplication app, string title, string version = "v1", IReadOnlyCollection<SwaggerDocumentEndpoint>? additionalDocuments = null)` — used by Task 2 (Sales.Api) and unchanged for Task 3 (Inventory.Api, which keeps calling it with no third argument).

- [ ] **Step 1: Create the new test project**

Create `tests/BuildingBlocks.Web.Tests/BuildingBlocks.Web.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><IsPackable>false</IsPackable><IsTestProject>true</IsTestProject></PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4"><PrivateAssets>all</PrivateAssets></PackageReference>
    <PackageReference Include="Microsoft.AspNetCore.TestHost" Version="10.0.0" />
  </ItemGroup>
  <ItemGroup><Using Include="Xunit" /></ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Shared\BuildingBlocks.Web\BuildingBlocks.Web.csproj" />
  </ItemGroup>
</Project>
```

Register it in the solution:

```bash
dotnet sln Sales.sln add tests/BuildingBlocks.Web.Tests/BuildingBlocks.Web.Tests.csproj
```

- [ ] **Step 2: Write the failing test**

Create `tests/BuildingBlocks.Web.Tests/ApiDocumentationExtensionsTests.cs`:

```csharp
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

        var body = await client.GetStringAsync("/swagger/index.html");

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

        var body = await client.GetStringAsync("/swagger/index.html");

        Assert.Contains("/swagger/v1/swagger.json", body);
        Assert.Contains("Sales API v1", body);
        Assert.Contains("http://localhost:5001/swagger/v1/swagger.json", body);
        Assert.Contains("Inventory API", body);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/BuildingBlocks.Web.Tests/BuildingBlocks.Web.Tests.csproj`
Expected: build FAILS — `SwaggerDocumentEndpoint` does not exist yet and `UseApiDocumentation` has no `additionalDocuments` parameter.

- [ ] **Step 4: Create the `SwaggerDocumentEndpoint` record**

Create `src/Shared/BuildingBlocks.Web/OpenApi/SwaggerDocumentEndpoint.cs`:

```csharp
namespace BuildingBlocks.Web.OpenApi;

/// <summary>
/// Identifies one Swagger/OpenAPI document to list in a Swagger UI's document picker.
/// </summary>
/// <param name="DisplayName">
/// The label shown in the Swagger UI dropdown.
/// </param>
/// <param name="Url">
/// The absolute or relative URL of the document's <c>swagger.json</c>.
/// </param>
public sealed record SwaggerDocumentEndpoint(string DisplayName, string Url);
```

- [ ] **Step 5: Extend `UseApiDocumentation`**

In `src/Shared/BuildingBlocks.Web/OpenApi/ApiDocumentationExtensions.cs`, replace lines 67-97 (the `UseApiDocumentation` method and its doc comment) with:

```csharp
    /// <summary>
    /// Enables Swagger and Swagger UI in Development only.
    /// </summary>
    /// <param name="app">
    /// The application builder.
    /// </param>
    /// <param name="title">
    /// The API title displayed in Swagger UI.
    /// </param>
    /// <param name="version">
    /// The API version document name and display value.
    /// </param>
    /// <param name="additionalDocuments">
    /// Other Swagger/OpenAPI documents to list alongside this API's own document, e.g. another
    /// service's document fetched directly by the browser. Omit to preserve single-document behavior.
    /// </param>
    /// <returns>
    /// The same application builder, to allow chaining.
    /// </returns>
    public static WebApplication UseApiDocumentation(
        this WebApplication app,
        string title,
        string version = "v1",
        IReadOnlyCollection<SwaggerDocumentEndpoint>? additionalDocuments = null)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{title} {version}");

            foreach (var document in additionalDocuments ?? [])
            {
                options.SwaggerEndpoint(document.Url, document.DisplayName);
            }

            options.RoutePrefix = "swagger";
        });

        return app;
    }
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/BuildingBlocks.Web.Tests/BuildingBlocks.Web.Tests.csproj`
Expected: PASS (2 tests).

- [ ] **Step 7: Commit**

```bash
git add src/Shared/BuildingBlocks.Web/OpenApi/SwaggerDocumentEndpoint.cs \
        src/Shared/BuildingBlocks.Web/OpenApi/ApiDocumentationExtensions.cs \
        tests/BuildingBlocks.Web.Tests Sales.sln
git commit -m "feat: support multiple Swagger documents in UseApiDocumentation"
```

---

### Task 2: Sales.Api wires the Inventory Swagger document from configuration

**Files:**
- Create: `src/Services/Sales/Sales.Api/Extensions/SalesSwaggerDocumentsFactory.cs`
- Modify: `src/Services/Sales/Sales.Api/Extensions/ApplicationBuilderExtensions.cs:35`
- Modify: `src/Services/Sales/Sales.Api/appsettings.Development.json`
- Create: `tests/Sales.Api.Tests/Sales.Api.Tests.csproj`
- Create: `tests/Sales.Api.Tests/SalesSwaggerDocumentsFactoryTests.cs`
- Modify: `Sales.sln` (register the new test project)

**Interfaces:**
- Consumes: `BuildingBlocks.Web.OpenApi.SwaggerDocumentEndpoint(string DisplayName, string Url)` and `WebApplication.UseApiDocumentation(..., additionalDocuments: ...)` from Task 1.
- Produces: `Sales.Api.Extensions.SalesSwaggerDocumentsFactory.Create(IConfiguration configuration) : IReadOnlyCollection<SwaggerDocumentEndpoint>` — pure, no ASP.NET hosting required, easy to unit test.

- [ ] **Step 1: Create the test project**

Create `tests/Sales.Api.Tests/Sales.Api.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><IsPackable>false</IsPackable><IsTestProject>true</IsTestProject></PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4"><PrivateAssets>all</PrivateAssets></PackageReference>
  </ItemGroup>
  <ItemGroup><Using Include="Xunit" /></ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Services\Sales\Sales.Api\Sales.Api.csproj" />
  </ItemGroup>
</Project>
```

Register it in the solution:

```bash
dotnet sln Sales.sln add tests/Sales.Api.Tests/Sales.Api.Tests.csproj
```

- [ ] **Step 2: Write the failing test**

Create `tests/Sales.Api.Tests/SalesSwaggerDocumentsFactoryTests.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Sales.Api.Extensions;

namespace Sales.Api.Tests;

public sealed class SalesSwaggerDocumentsFactoryTests
{
    [Fact]
    public void Create_returns_empty_when_no_url_is_configured()
    {
        var configuration = new ConfigurationBuilder().Build();

        var documents = SalesSwaggerDocumentsFactory.Create(configuration);

        Assert.Empty(documents);
    }

    [Fact]
    public void Create_returns_the_inventory_document_when_url_is_configured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Swagger:InventoryApiUrl"] = "http://localhost:5001/swagger/v1/swagger.json"
            })
            .Build();

        var documents = SalesSwaggerDocumentsFactory.Create(configuration);

        var document = Assert.Single(documents);
        Assert.Equal("Inventory API", document.DisplayName);
        Assert.Equal("http://localhost:5001/swagger/v1/swagger.json", document.Url);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/Sales.Api.Tests/Sales.Api.Tests.csproj`
Expected: build FAILS — `SalesSwaggerDocumentsFactory` does not exist yet.

- [ ] **Step 4: Implement `SalesSwaggerDocumentsFactory`**

Create `src/Services/Sales/Sales.Api/Extensions/SalesSwaggerDocumentsFactory.cs`:

```csharp
using BuildingBlocks.Web.OpenApi;

namespace Sales.Api.Extensions;

/// <summary>
/// Builds the list of external Swagger documents that Sales.Api's aggregated Swagger UI should list,
/// read from configuration so BuildingBlocks.Web never needs to know application-specific URLs.
/// </summary>
public static class SalesSwaggerDocumentsFactory
{
    /// <summary>
    /// Reads <c>Swagger:InventoryApiUrl</c> from configuration and returns the matching
    /// <see cref="SwaggerDocumentEndpoint"/>, or an empty collection when it is not configured.
    /// </summary>
    public static IReadOnlyCollection<SwaggerDocumentEndpoint> Create(IConfiguration configuration)
    {
        var inventoryUrl = configuration["Swagger:InventoryApiUrl"];

        return string.IsNullOrWhiteSpace(inventoryUrl)
            ? []
            : [new SwaggerDocumentEndpoint("Inventory API", inventoryUrl)];
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests/Sales.Api.Tests/Sales.Api.Tests.csproj`
Expected: PASS (2 tests).

- [ ] **Step 6: Wire it into `ConfigureApplication`**

In `src/Services/Sales/Sales.Api/Extensions/ApplicationBuilderExtensions.cs:35`, replace:

```csharp
        app.UseApiDocumentation("Sales API");
```

with:

```csharp
        app.UseApiDocumentation(
            "Sales API",
            additionalDocuments: SalesSwaggerDocumentsFactory.Create(app.Configuration));
```

- [ ] **Step 7: Add the configuration value**

Read `src/Services/Sales/Sales.Api/appsettings.Development.json`, then replace its full contents with:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Swagger": {
    "InventoryApiUrl": "http://localhost:5001/swagger/v1/swagger.json"
  }
}
```

- [ ] **Step 8: Build to confirm everything compiles**

Run: `dotnet build src/Services/Sales/Sales.Api/Sales.Api.csproj`
Expected: build succeeds with no errors.

- [ ] **Step 9: Commit**

```bash
git add src/Services/Sales/Sales.Api/Extensions/SalesSwaggerDocumentsFactory.cs \
        src/Services/Sales/Sales.Api/Extensions/ApplicationBuilderExtensions.cs \
        src/Services/Sales/Sales.Api/appsettings.Development.json \
        tests/Sales.Api.Tests Sales.sln
git commit -m "feat: list Inventory's Swagger document in Sales.Api's Swagger UI"
```

---

### Task 3: Inventory.Api Development-only CORS for the aggregated Swagger UI

**Files:**
- Create: `src/Services/Inventory/Inventory.Api/Extensions/SwaggerCorsExtensions.cs`
- Modify: `src/Services/Inventory/Inventory.Api/Extensions/ServiceCollectionExtensions.cs:31-33` (after `AddApiDocumentation`)
- Modify: `src/Services/Inventory/Inventory.Api/Extensions/ApplicationBuilderExtensions.cs:23-28`
- Create: `tests/Inventory.Api.Tests/Inventory.Api.Tests.csproj`
- Create: `tests/Inventory.Api.Tests/SwaggerCorsExtensionsTests.cs`
- Modify: `Sales.sln` (register the new test project)

**Interfaces:**
- Produces: `Inventory.Api.Extensions.SwaggerCorsExtensions.PolicyName : string` (constant), `AddSwaggerCors(this IServiceCollection services, IHostEnvironment environment) : IServiceCollection`, `UseSwaggerCors(this WebApplication app) : WebApplication`.
- No dependency on Sales.Api anywhere in this task.

- [ ] **Step 1: Create the test project**

Create `tests/Inventory.Api.Tests/Inventory.Api.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><IsPackable>false</IsPackable><IsTestProject>true</IsTestProject></PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4"><PrivateAssets>all</PrivateAssets></PackageReference>
  </ItemGroup>
  <ItemGroup><Using Include="Xunit" /></ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Services\Inventory\Inventory.Api\Inventory.Api.csproj" />
  </ItemGroup>
</Project>
```

Register it in the solution:

```bash
dotnet sln Sales.sln add tests/Inventory.Api.Tests/Inventory.Api.Tests.csproj
```

- [ ] **Step 2: Write the failing test**

Create `tests/Inventory.Api.Tests/SwaggerCorsExtensionsTests.cs`:

```csharp
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
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/Inventory.Api.Tests/Inventory.Api.Tests.csproj`
Expected: build FAILS — `SwaggerCorsExtensions` does not exist yet.

- [ ] **Step 4: Implement `SwaggerCorsExtensions`**

Create `src/Services/Inventory/Inventory.Api/Extensions/SwaggerCorsExtensions.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inventory.Api.Extensions;

/// <summary>
/// Development-only CORS policy that lets the aggregated Swagger UI hosted by Sales.Api fetch
/// Inventory.Api's OpenAPI document and execute "Try it out" requests directly from the browser.
/// </summary>
public static class SwaggerCorsExtensions
{
    /// <summary>
    /// The name of the CORS policy registered for the aggregated Swagger UI.
    /// </summary>
    public const string PolicyName = "AggregatedSwaggerUi";

    private static readonly string[] AllowedOrigins =
    [
        "http://localhost:5000",
        "https://localhost:5002"
    ];

    /// <summary>
    /// Registers the CORS policy in Development only; a no-op in other environments.
    /// </summary>
    public static IServiceCollection AddSwaggerCors(this IServiceCollection services, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return services;
        }

        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }

    /// <summary>
    /// Applies the CORS policy in Development only; a no-op in other environments.
    /// </summary>
    public static WebApplication UseSwaggerCors(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.UseCors(PolicyName);
        return app;
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests/Inventory.Api.Tests/Inventory.Api.Tests.csproj`
Expected: PASS (2 tests).

- [ ] **Step 6: Register the policy in `AddApplicationServices`**

In `src/Services/Inventory/Inventory.Api/Extensions/ServiceCollectionExtensions.cs:41`, replace:

```csharp
        builder.Services.AddApiDocumentation(
            "Inventory API",
            "Inventory service API for stock queries, reservations, and stock adjustments.");
```

with:

```csharp
        builder.Services.AddApiDocumentation(
            "Inventory API",
            "Inventory service API for stock queries, reservations, and stock adjustments.");
        builder.Services.AddSwaggerCors(builder.Environment);
```

- [ ] **Step 7: Apply the policy in `ConfigureApplication` with the required middleware order**

In `src/Services/Inventory/Inventory.Api/Extensions/ApplicationBuilderExtensions.cs`, replace lines 23-28:

```csharp
        app.UseSerilogRequestLogging(RequestLoggingDefaults.Configure);
        app.UseMiddleware<RequestObservabilityMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseApiDocumentation("Inventory API");
        app.MapControllers();
```

with:

```csharp
        app.UseSerilogRequestLogging(RequestLoggingDefaults.Configure);
        app.UseMiddleware<RequestObservabilityMiddleware>();
        app.UseRouting();
        app.UseSwaggerCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseApiDocumentation("Inventory API");
        app.MapControllers();
```

- [ ] **Step 8: Build to confirm everything compiles**

Run: `dotnet build src/Services/Inventory/Inventory.Api/Inventory.Api.csproj`
Expected: build succeeds with no errors.

- [ ] **Step 9: Commit**

```bash
git add src/Services/Inventory/Inventory.Api/Extensions/SwaggerCorsExtensions.cs \
        src/Services/Inventory/Inventory.Api/Extensions/ServiceCollectionExtensions.cs \
        src/Services/Inventory/Inventory.Api/Extensions/ApplicationBuilderExtensions.cs \
        tests/Inventory.Api.Tests Sales.sln
git commit -m "feat: enable Development-only CORS for the aggregated Swagger UI"
```

---

### Task 4: Manual end-to-end verification

**Files:** none (no code changes — this task only exercises the running services).

**Interfaces:** none.

This feature's real acceptance test is a browser interaction that unit tests cannot cover: does the dropdown actually appear, does selecting "Inventory API" load real data over the network, and does "Try it out" actually reach Inventory.Api. Run through this checklist and treat any failure as a bug in Tasks 1-3, not something to patch here.

- [ ] **Step 1: Start both services**

If Docker is available:

```bash
docker compose -f docker/docker-compose.yml up -d --build sales-api inventory-api
```

If Docker is not available, run both from source in two terminals (Inventory.Api's `launchSettings.json` defaults to a random port, so pin it to 5001 explicitly to match the configured URL):

```bash
dotnet run --project src/Services/Sales/Sales.Api/Sales.Api.csproj --launch-profile http
dotnet run --project src/Services/Inventory/Inventory.Api/Inventory.Api.csproj --urls http://localhost:5001
```

- [ ] **Step 2: Confirm Inventory's standalone Swagger still works**

Open `http://localhost:5001/swagger` in a browser.
Expected: the existing Inventory API Swagger UI loads exactly as before, single document.

- [ ] **Step 3: Confirm Sales' aggregated Swagger UI**

Open `http://localhost:5000/swagger` in a browser.
Expected: a document-picker dropdown near the top-right lists two entries: "Sales API v1" and "Inventory API".

- [ ] **Step 4: Confirm the Inventory document loads**

In that dropdown, select "Inventory API".
Expected: the page reloads its endpoint list to show Inventory's controllers/schemas (e.g. stock queries, reservations, adjustments), with no CORS error in the browser devtools console.

- [ ] **Step 5: Confirm "Try it out" reaches Inventory.Api**

With "Inventory API" selected, expand any `GET` endpoint that doesn't require auth, click "Try it out", then "Execute".
Expected: a real HTTP response comes back from Inventory.Api (check the request URL in devtools Network tab resolves to `localhost:5001`), with no CORS error blocking the response.

- [ ] **Step 6: Confirm the local Sales document is unaffected**

Switch the dropdown back to "Sales API v1".
Expected: Sales' own endpoints (auth, products, customers, orders) list and "Try it out" exactly as they did before this change.
