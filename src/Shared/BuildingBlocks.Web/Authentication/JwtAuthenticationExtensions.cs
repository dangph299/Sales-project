using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Web.Authentication;

/// <summary>
/// Shared JWT bearer authentication registration for HTTP API hosts.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Registers JWT bearer authentication using the solution's shared <c>Jwt</c> configuration section.
    /// </summary>
    /// <param name="services">
    /// The service collection to register into.
    /// </param>
    /// <param name="configuration">
    /// The application configuration.
    /// </param>
    /// <param name="clockSkew">
    /// Optional token clock skew override. When omitted, ASP.NET Core's default is preserved.
    /// </param>
    /// <returns>
    /// The same service collection, to allow chaining.
    /// </returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        TimeSpan? clockSkew = null)
    {
        var key = configuration["Jwt:Key"] ?? "development-only-key-change-me-32-chars";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "sales-api",
                    ValidAudience = configuration["Jwt:Audience"] ?? "sales-clients",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                };

                if (clockSkew.HasValue)
                {
                    options.TokenValidationParameters.ClockSkew = clockSkew.Value;
                }
            });

        return services;
    }
}
