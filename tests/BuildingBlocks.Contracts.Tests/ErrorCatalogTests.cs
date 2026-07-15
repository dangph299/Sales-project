using System.Reflection;
using Inventory.Api.Middleware;

namespace BuildingBlocks.Contracts.Tests;

public sealed class ErrorCatalogTests
{
    [Fact]
    public void Error_code_constants_are_unique()
    {
        var values = ErrorCodeValues();

        Assert.Equal(values.Count, values.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Every_error_code_constant_has_catalog_definition()
    {
        var catalogCodes = ErrorCatalog.All.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);

        foreach (var code in ErrorCodeValues())
            Assert.Contains(code, catalogCodes);
    }

    [Fact]
    public void Unknown_code_resolves_safely_to_internal_server_error()
    {
        var definition = ErrorCatalog.Get("unknown_code");

        Assert.Equal(ErrorCodes.InternalServerError, definition.Code);
    }

    [Fact]
    public void Default_provider_returns_default_description()
    {
        var provider = new DefaultErrorMessageProvider();

        var description = provider.GetDescription(ErrorCodes.NotFound, ErrorCatalog.NotFound.Description);

        Assert.Equal(ErrorCatalog.NotFound.Description, description);
    }

    [Fact]
    public void Service_provider_overrides_description_without_changing_code()
    {
        var catalog = new ErrorCatalogResolver(new InventoryErrorMessageProvider());

        var definition = catalog.Get(ErrorCodes.NotFound);

        Assert.Equal(ErrorCodes.NotFound, definition.Code);
        Assert.Equal("The requested inventory resource was not found.", definition.Description);
    }

    [Fact]
    public void Service_provider_uses_default_description_for_non_overridden_code()
    {
        var catalog = new ErrorCatalogResolver(new InventoryErrorMessageProvider());

        var definition = catalog.Get(ErrorCodes.UniqueViolation);

        Assert.Equal(ErrorCodes.UniqueViolation, definition.Code);
        Assert.Equal(ErrorCatalog.UniqueViolation.Description, definition.Description);
    }

    private static List<string> ErrorCodeValues()
    {
        return typeof(ErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(x => x is { IsLiteral: true, IsInitOnly: false } && x.FieldType == typeof(string))
            .Select(x => (string)x.GetRawConstantValue()!)
            .ToList();
    }
}
