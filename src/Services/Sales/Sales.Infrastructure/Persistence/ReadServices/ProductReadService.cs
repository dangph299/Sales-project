using Microsoft.EntityFrameworkCore;
using MapsterMapper;
using Sales.Application.Features.Products.DTOs;
using Sales.Application.Features.Products.Interfaces;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Read-side product lookup service for query handlers.
/// </summary>
public sealed class ProductReadService(SalesDbContext db) : IProductReadService
{
    public ProductReadService(SalesDbContext db, IMapper mapper)
        : this(db)
    {
    }

    /// <inheritdoc/>
    public Task<ProductDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return LoadAsync(id, publishedOnly: true, ct);
    }

    /// <inheritdoc/>
    public Task<ProductDto?> GetForWriteResultAsync(Guid id, CancellationToken ct = default)
    {
        return LoadAsync(id, publishedOnly: false, ct);
    }

    private async Task<ProductDto?> LoadAsync(Guid id, bool publishedOnly, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == id && !x.IsDelete,
                ct);
        if (product is null)
        {
            return null;
        }

        var category = await db.Categories.AsNoTracking().SingleAsync(x => x.Id == product.CategoryId, ct);
        var variants = await LoadVariants([product.Id], ct);
        var dto = MapProduct(product, category, variants.GetValueOrDefault(product.Id, []));
        return !publishedOnly || dto.IsActive ? dto : null;
    }

    /// <inheritdoc/>
    public Task<PagedResult<ProductDto>> SearchAsync(
        string? name,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(null, name, null, null, null, null, null, page, pageSize, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<ProductDto>> SearchAsync(
        string? productCode,
        string? name,
        Guid? categoryId,
        string? sku,
        Guid? colorId,
        Guid? sizeId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var query = db.Products.AsNoTracking().Where(x => !x.IsDelete);

        if (!string.IsNullOrWhiteSpace(productCode))
        {
            var normalizedProductCode = ProductCodeRules.Normalize(productCode, "Product code");
            query = query.Where(x => x.ProductCode == normalizedProductCode);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{name.Trim()}%"));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EProductStatus>(status, ignoreCase: true, out var productStatus))
        {
            query = query.Where(x => x.Status == productStatus);
        }

        if (!string.IsNullOrWhiteSpace(sku) || colorId.HasValue || sizeId.HasValue)
        {
            var variantQuery = db.ProductVariants.AsNoTracking().Where(x => !x.IsDelete);
            if (!string.IsNullOrWhiteSpace(sku))
            {
                var normalizedSku = ProductCodeRules.Normalize(sku, "SKU");
                variantQuery = variantQuery.Where(x => x.Sku == normalizedSku);
            }

            if (colorId.HasValue)
            {
                variantQuery = variantQuery.Where(x => x.ColorId == colorId.Value);
            }

            if (sizeId.HasValue)
            {
                variantQuery = variantQuery.Where(x => x.SizeId == sizeId.Value);
            }

            query = query.Where(product => variantQuery.Any(variant => variant.ProductId == product.Id));
        }

        var total = await query.LongCountAsync(ct);
        var products = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var productIds = products.Select(x => x.Id).ToArray();
        var categoryIds = products.Select(x => x.CategoryId).Distinct().ToArray();
        var categories = await db.Categories.AsNoTracking()
            .Where(x => categoryIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var variants = await LoadVariants(productIds, ct);

        var productDtos = products
            .Select(product => MapProduct(product, categories[product.CategoryId], variants.GetValueOrDefault(product.Id, [])))
            .ToArray();

        return new(productDtos, page, pageSize, total);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<ProductVariantLookupDto>> SearchVariantsAsync(
        string? productCode,
        string? productName,
        string? sku,
        string? variantStatus,
        string? sortBy,
        string? sortDirection,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);

        var query =
            from variant in db.ProductVariants.AsNoTracking()
            join product in db.Products.AsNoTracking() on variant.ProductId equals product.Id
            join color in db.Colors.AsNoTracking() on variant.ColorId equals color.Id
            join size in db.Sizes.AsNoTracking() on variant.SizeId equals size.Id
            where !product.IsDelete
            select new
            {
                Product = product,
                Variant = variant,
                Color = color,
                Size = size
            };

        if (!string.IsNullOrWhiteSpace(productCode))
        {
            var normalizedProductCode = ProductCodeRules.Normalize(productCode, "Product code");
            query = query.Where(x => x.Product.ProductCode == normalizedProductCode);
        }

        if (!string.IsNullOrWhiteSpace(productName))
        {
            var pattern = $"%{productName.Trim()}%";
            query = db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL"
                ? query.Where(x => EF.Functions.ILike(x.Product.Name, pattern))
                : query.Where(x => EF.Functions.Like(x.Product.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(sku))
        {
            var normalizedSku = ProductCodeRules.Normalize(sku, "SKU");
            query = query.Where(x => x.Variant.Sku == normalizedSku);
        }

        if (!string.IsNullOrWhiteSpace(variantStatus) &&
            Enum.TryParse<EProductVariantStatus>(variantStatus, ignoreCase: true, out var parsedVariantStatus))
        {
            query = query.Where(x => x.Variant.Status == parsedVariantStatus);
        }

        var total = await query.LongCountAsync(ct);
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sortDirection, "descend", StringComparison.OrdinalIgnoreCase);
        var ordered = (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "productcode" => descending
                ? query.OrderByDescending(x => x.Product.ProductCode).ThenBy(x => x.Variant.Id)
                : query.OrderBy(x => x.Product.ProductCode).ThenBy(x => x.Variant.Id),
            "product" or "productname" => descending
                ? query.OrderByDescending(x => x.Product.Name).ThenBy(x => x.Variant.Id)
                : query.OrderBy(x => x.Product.Name).ThenBy(x => x.Variant.Id),
            "color" => descending
                ? query.OrderByDescending(x => x.Color.ColorCode).ThenBy(x => x.Variant.Id)
                : query.OrderBy(x => x.Color.ColorCode).ThenBy(x => x.Variant.Id),
            "size" => descending
                ? query.OrderByDescending(x => x.Size.SortOrder).ThenByDescending(x => x.Size.Code).ThenBy(x => x.Variant.Id)
                : query.OrderBy(x => x.Size.SortOrder).ThenBy(x => x.Size.Code).ThenBy(x => x.Variant.Id),
            "status" or "variantstatus" => descending
                ? query.OrderByDescending(x => x.Variant.Status).ThenBy(x => x.Variant.Id)
                : query.OrderBy(x => x.Variant.Status).ThenBy(x => x.Variant.Id),
            _ => descending
                ? query.OrderByDescending(x => x.Variant.Sku).ThenBy(x => x.Variant.Id)
                : query.OrderBy(x => x.Variant.Sku).ThenBy(x => x.Variant.Id)
        };
        var rows = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductVariantLookupDto(
                x.Product.Id,
                x.Product.ProductCode,
                x.Product.Name,
                x.Product.Status.ToString(),
                x.Variant.Id,
                x.Variant.Sku,
                new ProductColorDto(x.Color.Id, x.Color.ColorCode, x.Color.Name, x.Color.HexCode),
                new ProductSizeDto(x.Size.Id, x.Size.Code, x.Size.Name),
                x.Variant.Price.Amount,
                x.Variant.Status.ToString()))
            .ToArrayAsync(ct);

        return new(rows, page, pageSize, total);
    }

    private async Task<Dictionary<Guid, (ProductVariant Variant, Color Color, Size Size)[]>> LoadVariants(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var variantRows = await db.ProductVariants.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId) && !x.IsDelete)
            .Join(db.Colors.AsNoTracking(), variant => variant.ColorId, color => color.Id, (variant, color) => new { variant, color })
            .Join(db.Sizes.AsNoTracking(), pair => pair.variant.SizeId, size => size.Id, (pair, size) => new { pair.variant, pair.color, size })
            .ToListAsync(cancellationToken);

        return variantRows
            .GroupBy(x => x.variant.ProductId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(row => (row.variant, row.color, row.size)).ToArray());
    }

    private static ProductDto MapProduct(
        Product product,
        Category category,
        IReadOnlyCollection<(ProductVariant Variant, Color Color, Size Size)> variants)
    {
        var activeVariant = variants
            .Where(x => x.Variant.Status == EProductVariantStatus.Published)
            .OrderBy(x => x.Variant.Sku)
            .FirstOrDefault();
        var sku = activeVariant.Variant?.Sku ?? product.ProductCode;
        var publishedVariantPrices = variants
            .Where(x => x.Variant.Status == EProductVariantStatus.Published)
            .Select(x => x.Variant.Price.Amount)
            .ToArray();
        decimal? minPrice = publishedVariantPrices.Length == 0 ? null : publishedVariantPrices.Min();
        decimal? maxPrice = publishedVariantPrices.Length == 0 ? null : publishedVariantPrices.Max();

        return new ProductDto(
            product.Id,
            sku,
            product.Name,
            minPrice,
            maxPrice,
            publishedVariantPrices.Length > 0 && !product.IsDelete,
            product.Version,
            product.CreatedAt,
            product.UpdatedAt,
            product.IsDelete,
            product.DeleteByUser,
            product.DeletedAt)
        {
            ProductCode = product.ProductCode,
            Description = product.Description,
            CategoryId = product.CategoryId,
            Status = product.Status.ToString(),
            Category = new ProductCategoryDto(category.Id, category.CategoryCode, category.Name),
            Variants = variants
                .OrderBy(x => x.Variant.Sku)
                .Select(x => new ProductVariantDto(
                    x.Variant.Id,
                    x.Variant.Sku,
                    new ProductColorDto(x.Color.Id, x.Color.ColorCode, x.Color.Name, x.Color.HexCode),
                    new ProductSizeDto(x.Size.Id, x.Size.Code, x.Size.Name),
                    x.Variant.Price.Amount,
                    x.Variant.Status.ToString(),
                    x.Variant.CreatedAt,
                    x.Variant.UpdatedAt))
                .ToArray()
        };
    }
}
