namespace Inventory.Api.Extensions;

/// <summary>
/// Configures Development-only CORS for the aggregated Swagger UI hosted by Sales.Api.
/// </summary>
public static class SwaggerCorsExtensions
{
    /// <summary>
    /// The named CORS policy used for aggregated Swagger UI access.
    /// </summary>
    public const string PolicyName = "AggregatedSwaggerUi";

    private static readonly string[] AllowedOrigins =
    [
        "http://localhost:5000",
        "https://localhost:5002"
    ];

    /// <summary>
    /// Registers the CORS policy in Development only.
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
    /// Applies the CORS policy in Development only.
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
