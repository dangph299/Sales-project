using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Default aggregate resolver that audits each changed entity under its own primary key.
/// </summary>
public sealed class DefaultAuditAggregateResolver : IAuditAggregateResolver
{
    /// <inheritdoc/>
    public AuditAggregateIdentity Resolve(EntityEntry entityEntry)
    {
        var entityType = entityEntry.Metadata.ClrType.Name;
        var entityId = BuildEntityId(entityEntry);
        return new AuditAggregateIdentity(entityType, entityId, TryParseGuid(entityId), string.Empty);
    }

    private static string BuildEntityId(EntityEntry entityEntry)
    {
        var primaryKey = entityEntry.Metadata.FindPrimaryKey();
        if (primaryKey is null)
        {
            return entityEntry.Entity.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var values = primaryKey.Properties
            .Select(property =>
            {
                var propertyEntry = entityEntry.Property(property.Name);
                var value = propertyEntry.CurrentValue ?? propertyEntry.OriginalValue;
                return $"{property.Name}={value}";
            })
            .ToArray();

        return values.Length == 1 ? values[0].Split('=', 2)[1] : string.Join("|", values);
    }

    private static Guid? TryParseGuid(string value)
    {
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
