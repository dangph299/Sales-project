using System.Collections;
using System.Reflection;
using BuildingBlocks.Contracts;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Builds <see cref="AuditChange"/> lists for audit events by reflecting over
/// objects and flattening nested properties into dotted field paths.
/// </summary>
public static class AuditChangeDetector
{
    /// <summary>
    /// Builds the changes representing an entity's initial values.
    /// </summary>
    /// <param name="value">Entity's initial values, typically an anonymous object.</param>
    /// <param name="displayNames">An optional map from field name to human-readable label.</param>
    /// <returns>One change per flattened field, with <c>OldValue</c> null and <c>NewValue</c> set.</returns>
    public static IReadOnlyCollection<AuditChange> Created<T>(T value, IReadOnlyDictionary<string, string>? displayNames = null) =>
        Flatten(value)
            .Select(x => ToChange(x.Key, displayNames, null, x.Value))
            .ToArray();

    /// <summary>
    /// Builds the changes representing an entity's final values before deletion.
    /// </summary>
    /// <param name="value">Entity's values at the time of deletion, typically an anonymous object.</param>
    /// <param name="displayNames">An optional map from field name to human-readable label.</param>
    /// <returns>One change per flattened field, with <c>OldValue</c> set and <c>NewValue</c> null.</returns>
    public static IReadOnlyCollection<AuditChange> Deleted<T>(T value, IReadOnlyDictionary<string, string>? displayNames = null) =>
        Flatten(value)
            .Select(x => ToChange(x.Key, displayNames, x.Value, null))
            .ToArray();

    /// <summary>
    /// Diffs two snapshots of an entity and builds the changes for every field whose value differs.
    /// </summary>
    /// <param name="before">Entity's values before the update, typically an anonymous object.</param>
    /// <param name="after">Entity's values after the update, typically an anonymous object.</param>
    /// <param name="displayNames">An optional map from field name to human-readable label.</param>
    /// <returns>One change per field whose flattened value differs between <paramref name="before"/> and <paramref name="after"/>. Unchanged fields are omitted.</returns>
    public static IReadOnlyCollection<AuditChange> Updated<TBefore, TAfter>(
        TBefore before,
        TAfter after,
        IReadOnlyDictionary<string, string>? displayNames = null)
    {
        var oldValues = Flatten(before);
        var newValues = Flatten(after);

        return oldValues.Keys
            .Union(newValues.Keys)
            .OrderBy(x => x, StringComparer.Ordinal)
            .Where(field => !Equals(Normalize(oldValues.GetValueOrDefault(field)), Normalize(newValues.GetValueOrDefault(field))))
            .Select(field => ToChange(field, displayNames, oldValues.GetValueOrDefault(field), newValues.GetValueOrDefault(field)))
            .ToArray();
    }

    /// <summary>
    /// Builds a single field-level change, normalizing values and inferring a data type hint when not supplied.
    /// </summary>
    /// <param name="field">Changed field name.</param>
    /// <param name="oldValue">Field's value before the change, or <see langword="null"/>.</param>
    /// <param name="newValue">Field's value after the change, or <see langword="null"/>.</param>
    /// <param name="displayName">An optional human-readable label for the field.</param>
    /// <param name="dataType">An optional explicit data type hint; inferred from <paramref name="oldValue"/>/<paramref name="newValue"/> if not supplied.</param>
    /// <returns>Change record.</returns>
    public static AuditChange Change(string field, object? oldValue, object? newValue, string? displayName = null, string? dataType = null)
    {
        return new AuditChange
        {
            PropertyPath = field,
            OldValue = Normalize(oldValue),
            NewValue = Normalize(newValue)
        };
    }

    private static AuditChange ToChange(string field, IReadOnlyDictionary<string, string>? displayNames, object? oldValue, object? newValue) =>
        Change(field, oldValue, newValue, displayNames?.GetValueOrDefault(field));

    private static IReadOnlyDictionary<string, object?> Flatten(object? value)
    {
        var result = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        FlattenInto(result, null, value);
        return result;
    }

    private static void FlattenInto(IDictionary<string, object?> result, string? prefix, object? value)
    {
        if (value is null)
        {
            if (prefix is not null) result[prefix] = null;
            return;
        }

        var type = value.GetType();
        if (IsScalar(type) || value is IEnumerable and not string)
        {
            if (prefix is not null) result[prefix] = Normalize(value);
            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0) continue;
            var field = prefix is null ? property.Name : $"{prefix}.{property.Name}";
            var propertyValue = property.GetValue(value);
            if (propertyValue is null || IsScalar(property.PropertyType) || propertyValue is IEnumerable and not string)
            {
                result[field] = Normalize(propertyValue);
                continue;
            }

            FlattenInto(result, field, propertyValue);
        }
    }

    private static object? Normalize(object? value) =>
        value switch
        {
            null => null,
            decimal x => x,
            DateTimeOffset x => x,
            DateTime x => x,
            Guid x => x,
            Enum x => x.ToString(),
            string x => x,
            IEnumerable values and not string => values.Cast<object?>().Select(Normalize).ToArray(),
            _ => value
        };

    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(Guid) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset);
    }

    private static string? DataType(object? value)
    {
        if (value is null) return null;
        var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
        if (type == typeof(string) || type == typeof(Guid)) return "string";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "decimal";
        if (type.IsPrimitive) return "number";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return "datetime";
        if (type.IsEnum) return "string";
        if (value is IEnumerable and not string) return "array";
        return "object";
    }
}
