namespace Sales.Domain;

/// <summary>
/// Aggregate root for shared catalog product information. Variants owned by this aggregate carry
/// purchasable SKU, color, size, and price data.
/// </summary>
public sealed class Product : AggregateRoot<Guid>
{
    private readonly List<ProductVariant> _variants = [];

    private Product() { }

    private Product(Guid id, string productCode, string name, string? description, Guid categoryId)
    {
        Id = id;
        ProductCode = ProductCodeRules.Normalize(productCode, "Product code");
        ChangeDetails(name, description, categoryId);
        Status = EProductStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string ProductCode { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public Guid CategoryId { get; private set; }

    public EProductStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public string? UpdatedBy { get; private set; }

    public bool IsDelete { get; private set; }

    public string? DeleteByUser { get; private set; }

    public string? DeletedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    public string Sku => ActiveVariant?.Sku ?? ProductCode;

    public bool IsActive => !IsDelete && _variants.Any(x => x.Status == EProductVariantStatus.Published && !x.IsDelete);

    public static Product Create(string productCode, string name, string? description, Guid categoryId)
    {
        var product = new Product(Guid.NewGuid(), productCode, name, description, categoryId);
        product.Raise(new ProductCreatedDomainEvent(product.Id, product.ProductCode, product.Name, product.CategoryId));
        return product;
    }

    public void Update(string name, string? description, Guid categoryId)
    {
        EnsureNotDeleted();

        var oldName = Name;
        var oldDescription = Description;
        var oldCategoryId = CategoryId;
        var oldStatus = Status;

        ChangeDetails(name, description, categoryId);

        if (oldName == Name && oldDescription == Description && oldCategoryId == CategoryId && oldStatus == Status)
        {
            return;
        }

        Touch();
        Raise(new ProductUpdatedDomainEvent(Id, oldName, oldCategoryId, oldStatus, Name, CategoryId, Status));
    }

    public void Publish()
    {
        EnsureNotDeleted();
        if (Status == EProductStatus.Published) return;
        if (Status is not (EProductStatus.Draft or EProductStatus.Discontinued))
            throw new DomainException("Only draft or discontinued products can be published.");

        Status = EProductStatus.Published;
        Touch();
    }

    public void Discontinue()
    {
        EnsureNotDeleted();
        if (Status == EProductStatus.Discontinued) return;
        if (Status != EProductStatus.Published)
            throw new DomainException("Only published products can be discontinued.");

        Status = EProductStatus.Discontinued;
        Touch();
    }

    public ProductVariant AddVariant(Color color, Size size, decimal price, EProductVariantStatus status = EProductVariantStatus.Draft)
    {
        EnsureNotDeleted();
        ArgumentNullException.ThrowIfNull(color);
        ArgumentNullException.ThrowIfNull(size);
        EnsureCanCreateVariant(status);
        EnsureVariantDoesNotExist(color.Id, size.Id, variantIdToIgnore: null);

        var variant = ProductVariant.Create(Id, ProductCode, color, size, price, status);
        _variants.Add(variant);
        Touch();
        return variant;
    }

    public void UpdateVariant(Guid variantId, Color color, Size size, decimal price, EProductVariantStatus status)
    {
        EnsureNotDeleted();
        ArgumentNullException.ThrowIfNull(color);
        ArgumentNullException.ThrowIfNull(size);

        var variant = FindVariant(variantId);
        EnsureVariantDoesNotExist(color.Id, size.Id, variantId);
        if (variant.Update(color, size, price, status))
        {
            Touch();
        }
    }

    public void PublishVariant(Guid variantId)
    {
        EnsureNotDeleted();
        if (FindVariant(variantId).Publish())
        {
            Touch();
        }
    }

    public void DiscontinueVariant(Guid variantId)
    {
        EnsureNotDeleted();
        if (FindVariant(variantId).Discontinue())
        {
            Touch();
        }
    }

    public void DeactivateVariant(Guid variantId)
    {
        DiscontinueVariant(variantId);
    }

    public void DeleteVariant(Guid variantId, string deleteByUser)
    {
        EnsureNotDeleted();
        if (FindVariant(variantId).Delete(deleteByUser))
        {
            Touch();
        }
    }

    public ProductVariant GetVariant(Guid variantId)
    {
        return FindVariant(variantId);
    }

    public void Delete(string deleteByUser)
    {
        if (IsDelete) return;
        var actor = string.IsNullOrWhiteSpace(deleteByUser) ? "system" : deleteByUser.Trim();
        IsDelete = true;
        DeleteByUser = actor;
        DeletedBy = actor;
        DeletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    private ProductVariant? ActiveVariant =>
        _variants
            .Where(x => x.Status == EProductVariantStatus.Published && !x.IsDelete)
            .OrderBy(x => x.Sku)
            .FirstOrDefault();

    private void ChangeDetails(string name, string? description, Guid categoryId)
    {
        if (categoryId == Guid.Empty) throw new DomainException("Product category is required.");

        Name = string.IsNullOrWhiteSpace(name) ? throw new DomainException("Product name is required.") : name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CategoryId = categoryId;
    }

    private ProductVariant FindVariant(Guid variantId)
    {
        return _variants.SingleOrDefault(x => x.Id == variantId) ??
            throw new DomainException("Product variant was not found.");
    }

    private void EnsureVariantDoesNotExist(Guid colorId, Guid sizeId, Guid? variantIdToIgnore)
    {
        if (_variants.Any(x => x.ColorId == colorId && x.SizeId == sizeId && x.Id != variantIdToIgnore && !x.IsDelete))
        {
            throw new DomainException("A variant with the same product, color, and size already exists.");
        }
    }

    private void EnsureNotDeleted()
    {
        if (IsDelete) throw new DomainException("Deleted products cannot be changed.");
    }

    private void EnsureCanCreateVariant(EProductVariantStatus status)
    {
        if (status == EProductVariantStatus.Discontinued)
        {
            throw new DomainException("New product variants cannot be discontinued.");
        }

    }
}
