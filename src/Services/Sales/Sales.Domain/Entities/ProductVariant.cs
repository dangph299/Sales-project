namespace Sales.Domain;

public sealed class ProductVariant : Entity<Guid>
{
    private ProductVariant() { }

    private ProductVariant(
        Guid id,
        Guid productId,
        Guid colorId,
        Guid sizeId,
        string sku,
        Money price,
        EProductVariantStatus status)
    {
        Id = id;
        ProductId = productId;
        ColorId = colorId;
        SizeId = sizeId;
        Sku = sku;
        Price = price;
        Status = status;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid ProductId { get; private set; }

    public Guid ColorId { get; private set; }

    public Guid SizeId { get; private set; }

    public string Sku { get; private set; } = null!;

    public Money Price { get; private set; }

    public EProductVariantStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string? UpdatedBy { get; private set; }

    public bool IsDelete { get; private set; }

    public string? DeleteByUser { get; private set; }

    public string? DeletedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public long Version { get; private set; } = 1;

    public static ProductVariant Create(
        Guid productId,
        string productCode,
        Color color,
        Size size,
        decimal price,
        EProductVariantStatus status)
    {
        if (productId == Guid.Empty) throw new DomainException("Product id is required.");
        if (!Enum.IsDefined(status)) throw new DomainException("Product variant status is invalid.");

        var sku = ProductCodeRules.BuildSku(productCode, color.ColorCode, size.Code);
        return new ProductVariant(Guid.NewGuid(), productId, color.Id, size.Id, sku, Money.Vnd(price), status);
    }

    public void Update(Color color, Size size, decimal price, EProductVariantStatus status)
    {
        EnsureNotDeleted();
        ArgumentNullException.ThrowIfNull(color);
        ArgumentNullException.ThrowIfNull(size);
        if (!Enum.IsDefined(status)) throw new DomainException("Product variant status is invalid.");
        EnsureCanChangeTo(status);

        ColorId = color.Id;
        SizeId = size.Id;
        Price = Money.Vnd(price);
        Status = status;
        Touch();
    }

    public void Publish()
    {
        EnsureNotDeleted();
        if (Status == EProductVariantStatus.Published) return;
        if (Status is not (EProductVariantStatus.Draft or EProductVariantStatus.Discontinued))
            throw new DomainException("Only draft or discontinued product variants can be published.");

        Status = EProductVariantStatus.Published;
        Touch();
    }

    public void Discontinue()
    {
        EnsureNotDeleted();
        if (Status == EProductVariantStatus.Discontinued) return;
        if (Status != EProductVariantStatus.Published)
            throw new DomainException("Only published product variants can be discontinued.");

        Status = EProductVariantStatus.Discontinued;
        Touch();
    }

    public void Delete(string deleteByUser)
    {
        if (IsDelete) return;
        if (Status is not (EProductVariantStatus.Draft or EProductVariantStatus.Discontinued))
        {
            throw new DomainException("Only draft or discontinued product variants can be deleted.");
        }

        var actor = string.IsNullOrWhiteSpace(deleteByUser) ? "system" : deleteByUser.Trim();
        IsDelete = true;
        DeleteByUser = actor;
        DeletedBy = actor;
        DeletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    private void Touch()
    {
        Version++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureNotDeleted()
    {
        if (IsDelete) throw new DomainException("Deleted product variants cannot be changed.");
    }

    private void EnsureCanChangeTo(EProductVariantStatus status)
    {
        if (status == Status) return;
        if (Status == EProductVariantStatus.Draft && status == EProductVariantStatus.Published) return;
        if (Status == EProductVariantStatus.Published && status == EProductVariantStatus.Discontinued) return;
        if (Status == EProductVariantStatus.Discontinued && status == EProductVariantStatus.Published) return;

        throw new DomainException("Product variant status transition is invalid.");
    }
}
