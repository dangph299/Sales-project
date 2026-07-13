namespace AuditLog.Infrastructure;

/// <summary>
/// Strongly-typed configuration for connecting to the audit MongoDB instance, bound from the
/// <c>Mongo</c> configuration section.
/// </summary>
public sealed class MongoOptions
{
    /// <summary>
    /// configuration section name this options class binds to.
    /// </summary>
    public const string SectionName = "Mongo";

    /// <summary>
    /// Gets or sets the MongoDB connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://mongo:27017";

    /// <summary>
    /// Gets or sets the name of the database to use.
    /// </summary>
    public string Database { get; set; } = "audit";
}
