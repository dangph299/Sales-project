namespace Sales.Infrastructure;

internal static class CategorySeedData
{
    private static readonly DateTimeOffset SeededAt =
        new(new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc));

    internal static readonly object Uncategorized = new
    {
        Id = CategoryReferenceDataIds.Uncategorized,
        CategoryCode = "CAT001",
        Name = "Uncategorized",
        Description = "Default category for products migrated from the legacy product schema.",
        ParentCategoryId = (Guid?)null,
        SortOrder = 0,
        Status = Sales.Domain.ECategoryStatus.Published,
        CreatedAt = SeededAt,
        CreatedBy = (string?)null,
        UpdatedAt = SeededAt,
        UpdatedBy = (string?)null,
        IsDelete = false,
        DeleteByUser = (string?)null,
        DeletedBy = (string?)null,
        DeletedAt = (DateTimeOffset?)null,
        Version = 1L
    };
}
