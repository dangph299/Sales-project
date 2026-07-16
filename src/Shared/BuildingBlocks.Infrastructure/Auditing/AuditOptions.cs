using System.Linq.Expressions;
using BuildingBlocks.Infrastructure;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Central audit configuration shared by EF audit generation.
/// </summary>
public sealed class AuditOptions
{
    private readonly HashSet<Type> _ignoredEntityTypes = [];
    private readonly HashSet<(Type EntityType, string PropertyName)> _ignoredProperties = [];
    private readonly HashSet<(Type EntityType, string PropertyName)> _maskedProperties = [];

    /// <summary>
    /// Gets or sets the service name stamped on emitted audit events.
    /// </summary>
    public string ServiceName { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the audit topic used by this service.
    /// </summary>
    public string TopicName { get; set; } = BuildingBlocks.Contracts.KafkaTopics.SalesAudit;

    /// <summary>
    /// Gets or sets the maximum string length stored in a change value.
    /// </summary>
    public int MaximumStringLength { get; set; } = 2_000;

    /// <summary>
    /// Gets or sets the maximum number of changes stored in a single audit event.
    /// </summary>
    public int MaximumChangesPerEvent { get; set; } = 100;

    /// <summary>
    /// Gets the ignored entity types.
    /// </summary>
    public IReadOnlySet<Type> IgnoredEntityTypes => _ignoredEntityTypes;

    /// <summary>
    /// Ignores an entity type during audit generation.
    /// </summary>
    public void IgnoreEntity<TEntity>()
    {
        _ignoredEntityTypes.Add(typeof(TEntity));
    }

    /// <summary>
    /// Ignores a property during audit generation.
    /// </summary>
    public void IgnoreProperty<TEntity>(Expression<Func<TEntity, object?>> property)
    {
        _ignoredProperties.Add((typeof(TEntity), GetPropertyName(property)));
    }

    /// <summary>
    /// Masks a property value during audit generation.
    /// </summary>
    public void MaskProperty<TEntity>(Expression<Func<TEntity, object?>> property)
    {
        _maskedProperties.Add((typeof(TEntity), GetPropertyName(property)));
    }

    /// <summary>
    /// Determines whether an entity type is ignored.
    /// </summary>
    public bool IsEntityIgnored(Type entityType)
    {
        return _ignoredEntityTypes.Contains(entityType);
    }

    /// <summary>
    /// Determines whether a property is ignored.
    /// </summary>
    public bool IsPropertyIgnored(Type entityType, string propertyName)
    {
        return _ignoredProperties.Contains((entityType, propertyName)) || IsTechnicalPropertyName(propertyName) || IsSensitivePropertyName(propertyName);
    }

    /// <summary>
    /// Determines whether a property value must be masked.
    /// </summary>
    public bool IsPropertyMasked(Type entityType, string propertyName)
    {
        return _maskedProperties.Contains((entityType, propertyName)) || IsSecretPropertyName(propertyName);
    }

    private static string GetPropertyName<TEntity>(Expression<Func<TEntity, object?>> property)
    {
        var expression = property.Body is UnaryExpression unaryExpression ? unaryExpression.Operand : property.Body;
        if (expression is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException("Expression must select a property.", nameof(property));
    }

    private static bool IsSensitivePropertyName(string propertyName)
    {
        return propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Token", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("ApiKey", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Payload", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTechnicalPropertyName(string propertyName)
    {
        return propertyName.Equals("Version", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("DeletedAt", StringComparison.OrdinalIgnoreCase)
            || propertyName.Equals("ReversedPhone", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSecretPropertyName(string propertyName)
    {
        return propertyName.Contains("Phone", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("Email", StringComparison.OrdinalIgnoreCase);
    }
}
