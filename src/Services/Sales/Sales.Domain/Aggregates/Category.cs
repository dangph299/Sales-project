namespace Sales.Domain;

public sealed class Category : AggregateRoot<Guid>
{
    private Category() { }

    private Category(Guid id, string categoryCode, string name, string? description, Guid? parentCategoryId, int sortOrder)
    {
        Id = id;
        CategoryCode = ProductCodeRules.Normalize(categoryCode, "Category code");
        ChangeDetails(name, description, parentCategoryId, sortOrder);
        Status = ECategoryStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string CategoryCode { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public Guid? ParentCategoryId { get; private set; }

    public int SortOrder { get; private set; }

    public ECategoryStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public string? UpdatedBy { get; private set; }

    public bool IsDelete { get; private set; }

    public string? DeleteByUser { get; private set; }

    public string? DeletedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public static Category Create(string categoryCode, string name, string? description, Guid? parentCategoryId, int sortOrder)
    {
        return new Category(Guid.NewGuid(), categoryCode, name, description, parentCategoryId, sortOrder);
    }

    public void Update(string name, string? description, Guid? parentCategoryId, int sortOrder)
    {
        EnsureNotDeleted();
        ChangeDetails(name, description, parentCategoryId, sortOrder);
        Touch();
    }

    public void Publish()
    {
        EnsureNotDeleted();
        if (Status == ECategoryStatus.Published) return;
        if (Status != ECategoryStatus.Draft) throw new DomainException("Only draft categories can be published.");

        Status = ECategoryStatus.Published;
        Touch();
    }

    public void Archive()
    {
        EnsureNotDeleted();
        if (Status == ECategoryStatus.Archived) return;
        if (Status != ECategoryStatus.Published) throw new DomainException("Only published categories can be archived.");

        Status = ECategoryStatus.Archived;
        Touch();
    }

    public void Delete(string deleteByUser)
    {
        if (IsDelete) return;
        var actor = string.IsNullOrWhiteSpace(deleteByUser) ? "system" : deleteByUser.Trim();
        IsDelete = true;
        DeleteByUser = actor;
        DeletedBy = actor;
        DeletedAt = DateTimeOffset.UtcNow;
        if (Status == ECategoryStatus.Published)
        {
            Status = ECategoryStatus.Archived;
        }

        Touch();
    }

    private void ChangeDetails(string name, string? description, Guid? parentCategoryId, int sortOrder)
    {
        if (parentCategoryId == Id) throw new DomainException("Category cannot be its own parent.");

        Name = string.IsNullOrWhiteSpace(name) ? throw new DomainException("Category name is required.") : name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ParentCategoryId = parentCategoryId;
        SortOrder = sortOrder;
    }

    private void EnsureNotDeleted()
    {
        if (IsDelete) throw new DomainException("Deleted categories cannot be changed.");
    }
}
