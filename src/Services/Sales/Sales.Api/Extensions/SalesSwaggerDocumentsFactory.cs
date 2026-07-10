using BuildingBlocks.Web.OpenApi;

namespace Sales.Api.Extensions;

/// <summary>
/// Builds the external Swagger documents listed by Sales.Api's Swagger UI.
/// </summary>
public static class SalesSwaggerDocumentsFactory
{
    /// <summary>
    /// Reads configured external Swagger documents for the aggregated Development UI.
    /// </summary>
    public static IReadOnlyCollection<SwaggerDocumentEndpoint> Create(IConfiguration configuration)
    {
        var inventoryUrl = configuration["Swagger:InventoryApiUrl"];

        return string.IsNullOrWhiteSpace(inventoryUrl)
            ? []
            : [new SwaggerDocumentEndpoint("Inventory API", inventoryUrl)];
    }
}
