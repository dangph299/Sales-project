namespace Sales.Domain;

public sealed class Size : Entity<Guid>
{
    private Size() { }

    private Size(Guid id, string code, string name, int sortOrder)
    {
        Id = id;
        Code = ProductCodeRules.Normalize(code, "Size code");
        Name = string.IsNullOrWhiteSpace(name) ? throw new DomainException("Size name is required.") : name.Trim();
        SortOrder = sortOrder;
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public int SortOrder { get; private set; }

    public static Size Create(Guid id, string code, string name, int sortOrder)
    {
        if (id == Guid.Empty) throw new DomainException("Size id is required.");
        return new Size(id, code, name, sortOrder);
    }
}
