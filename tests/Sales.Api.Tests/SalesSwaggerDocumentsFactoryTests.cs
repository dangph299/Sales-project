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
