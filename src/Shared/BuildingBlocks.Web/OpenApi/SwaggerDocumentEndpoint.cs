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
