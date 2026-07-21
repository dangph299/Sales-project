using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Sales.Application.Features.Orders.Realtime;

namespace Sales.Api.Realtime;

/// <summary>
/// Registers realtime services for Sales API.
/// </summary>
public static class RealtimeServiceCollectionExtensions
{
    internal const string SalesWebCorsPolicy = "SalesWeb";

    /// <summary>
    /// Adds SignalR order notifications and development CORS for Sales.Web.
    /// </summary>
    public static IServiceCollection AddSalesRealtime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSignalR()
            .AddJsonProtocol(options =>
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddScoped<IOrderRealtimeNotifier, SignalROrderRealtimeNotifier>();
        services.AddCors(options => options.AddPolicy(SalesWebCorsPolicy, policy => policy
            .WithOrigins(ReadAllowedOrigins(configuration))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));
        services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            ConfigureJwtBearerForSignalR);
        return services;
    }

    private static string[] ReadAllowedOrigins(IConfiguration configuration)
    {
        var origins = configuration
            .GetSection("SalesWeb:AllowedOrigins")
            .Get<string[]>();

        return origins is { Length: > 0 }
            ? origins
            :
            [
                "http://localhost:4200",
                "http://127.0.0.1:4200"
            ];
    }

    private static void ConfigureJwtBearerForSignalR(JwtBearerOptions options)
    {
        var originalOnMessageReceived = options.Events.OnMessageReceived;
        options.Events.OnMessageReceived = async context =>
        {
            if (originalOnMessageReceived is not null)
            {
                await originalOnMessageReceived(context);
            }

            if (!string.IsNullOrEmpty(context.Token))
            {
                return;
            }

            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/orders"))
            {
                context.Token = accessToken;
            }
        };
    }
}
