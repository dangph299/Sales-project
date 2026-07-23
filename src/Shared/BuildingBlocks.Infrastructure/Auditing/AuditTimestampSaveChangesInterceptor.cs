using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Normalizes persisted audit timestamp and actor columns for EF-tracked business entities.
/// </summary>
public sealed class AuditTimestampSaveChangesInterceptor(
    TimeProvider timeProvider,
    IAuditContextAccessor auditContextAccessor) : SaveChangesInterceptor
{
    private static readonly HashSet<string> AuditPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedAt",
        "CreatedOn",
        "CreatedDate",
        "CreationTime",
        "UpdatedAt",
        "UpdatedOn",
        "ModifiedAt",
        "LastUpdatedAt",
        "CreatedBy",
        "UpdatedBy",
        "ModifiedBy"
    };

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditTimestamps(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        dbContext.ChangeTracker.DetectChanges();
        var now = timeProvider.GetUtcNow();
        var actor = string.IsNullOrWhiteSpace(auditContextAccessor.ActorId)
            ? null
            : auditContextAccessor.ActorId.Trim();

        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified) || !HasAnyAuditProperty(entry))
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                ApplyAdded(entry, now, actor);
                continue;
            }

            ProtectCreateAudit(entry);
            if (HasBusinessChanges(entry))
            {
                ApplyModified(entry, now, actor);
            }
            else
            {
                ProtectUpdateAudit(entry);
            }
        }
    }

    private static bool HasAnyAuditProperty(EntityEntry entry) =>
        entry.Properties.Any(property => AuditPropertyNames.Contains(property.Metadata.Name));

    private static void ApplyAdded(EntityEntry entry, DateTimeOffset now, string? actor)
    {
        var createdAt = TryGetProperty(entry, "CreatedAt");
        if (createdAt is not null && IsDefaultDateTimeOffset(createdAt.CurrentValue))
        {
            createdAt.CurrentValue = now;
        }

        var updatedAt = TryGetProperty(entry, "UpdatedAt");
        if (updatedAt is not null && IsDefaultDateTimeOffset(updatedAt.CurrentValue))
        {
            updatedAt.CurrentValue = createdAt?.CurrentValue is DateTimeOffset created ? created : now;
        }

        SetActorIfEmpty(entry, "CreatedBy", actor);
        SetActorIfEmpty(entry, "UpdatedBy", actor);
    }

    private static void ApplyModified(EntityEntry entry, DateTimeOffset now, string? actor)
    {
        var updatedAt = TryGetProperty(entry, "UpdatedAt");
        if (updatedAt is not null)
        {
            updatedAt.CurrentValue = now;
            updatedAt.IsModified = true;
        }

        SetActor(entry, "UpdatedBy", actor);
    }

    private static void ProtectCreateAudit(EntityEntry entry)
    {
        SetUnmodified(entry, "CreatedAt");
        SetUnmodified(entry, "CreatedBy");
    }

    private static void ProtectUpdateAudit(EntityEntry entry)
    {
        SetUnmodified(entry, "UpdatedAt");
        SetUnmodified(entry, "UpdatedBy");
        SetUnmodified(entry, "ModifiedAt");
        SetUnmodified(entry, "ModifiedBy");
        SetUnmodified(entry, "LastUpdatedAt");
    }

    private static bool HasBusinessChanges(EntityEntry entry) =>
        entry.Properties.Any(property =>
            property.IsModified &&
            !property.Metadata.IsPrimaryKey() &&
            !property.Metadata.IsConcurrencyToken &&
            !AuditPropertyNames.Contains(property.Metadata.Name));

    private static PropertyEntry? TryGetProperty(EntityEntry entry, string propertyName) =>
        entry.Properties.SingleOrDefault(property => property.Metadata.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

    private static void SetUnmodified(EntityEntry entry, string propertyName)
    {
        var property = TryGetProperty(entry, propertyName);
        if (property is not null)
        {
            property.IsModified = false;
        }
    }

    private static void SetActorIfEmpty(EntityEntry entry, string propertyName, string? actor)
    {
        var property = TryGetProperty(entry, propertyName);
        if (property is null || !string.IsNullOrWhiteSpace(property.CurrentValue?.ToString()))
        {
            return;
        }

        property.CurrentValue = actor;
    }

    private static void SetActor(EntityEntry entry, string propertyName, string? actor)
    {
        if (actor is null)
        {
            return;
        }

        var property = TryGetProperty(entry, propertyName);
        if (property is null)
        {
            return;
        }

        property.CurrentValue = actor;
        property.IsModified = true;
    }

    private static bool IsDefaultDateTimeOffset(object? value) =>
        value is null || value is DateTimeOffset dateTimeOffset && dateTimeOffset == default;
}
