using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BuildingBlocks.Infrastructure;

namespace AuditLog.Infrastructure;

/// <summary>
/// Composition-root extensions for registering the AuditLog Infrastructure layer's services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="MongoOptions"/>, the MongoDB client/database, and <see cref="IAuditWriter"/>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration, used for the Mongo connection string and database name.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddAuditLogInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);
        services.Configure<MongoOptions>(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("Mongo") ?? options.ConnectionString;
            options.Database = configuration["Mongo:Database"] ?? options.Database;
        });
        services.AddSingleton<IMongoClient>(sp => new MongoClient(sp.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(sp.GetRequiredService<IOptions<MongoOptions>>().Value.Database));
        services.AddSingleton<IAuditWriter, MongoAuditWriter>();
        return services;
    }
}
