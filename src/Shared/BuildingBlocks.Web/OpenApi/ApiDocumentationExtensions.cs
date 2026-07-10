using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BuildingBlocks.Web.OpenApi;

/// <summary>
/// Shared Swagger/OpenAPI registration and middleware for controller-based API hosts.
/// </summary>
public static class ApiDocumentationExtensions
{
    /// <summary>
    /// Registers Swagger generation with common API metadata, XML documentation, and JWT support.
    /// </summary>
    /// <param name="services">
    /// The service collection to register into.
    /// </param>
    /// <param name="title">
    /// The API title displayed in Swagger.
    /// </param>
    /// <param name="description">
    /// The API description displayed in Swagger.
    /// </param>
    /// <param name="version">
    /// The API version document name and display value.
    /// </param>
    /// <returns>
    /// The same service collection, to allow chaining.
    /// </returns>
    public static IServiceCollection AddApiDocumentation(
        this IServiceCollection services,
        string title,
        string description,
        string version = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = description
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Paste a JWT access token. Swagger UI sends it as Authorization: Bearer {token}.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.OperationFilter<AuthorizeOperationFilter>();
            options.IncludeXmlCommentsIfAvailable(Assembly.GetEntryAssembly());
        });

        return services;
    }

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

    private static void IncludeXmlCommentsIfAvailable(this SwaggerGenOptions options, Assembly? assembly)
    {
        if (assembly is null)
        {
            return;
        }

        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }
}

internal sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var allowAnonymous = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any() ||
            context.MethodInfo.DeclaringType?
                .GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any() == true;

        if (allowAnonymous)
        {
            return;
        }

        var authorize = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Any() ||
            context.MethodInfo.DeclaringType?
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Any() == true;

        if (!authorize)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = []
        });
    }
}
