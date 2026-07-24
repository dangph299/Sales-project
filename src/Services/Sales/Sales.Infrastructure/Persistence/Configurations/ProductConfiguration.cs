using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Persistence mapping for catalog products.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        // Table
        entity.ToTable("products");

        // Primary Key
        entity.HasKey(x => x.Id);

        // Properties
        entity.Property(x => x.ProductCode).HasMaxLength(32);
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.CreatedBy).HasMaxLength(128);
        entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        entity.Property(x => x.DeleteByUser).HasMaxLength(128);
        entity.Property(x => x.DeletedBy).HasMaxLength(128);
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.Ignore(x => x.Sku);
        entity.Ignore(x => x.IsActive);

        // Relationships
        entity.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(x => x.Variants)
            .WithOne()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(x => x.Variants).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        // Ignore soft-deleted rows so deleting a product releases its code for reuse.
        entity.HasIndex(x => x.ProductCode).IsUnique().HasFilter("NOT \"IsDelete\"");
        entity.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        entity.HasIndex(x => x.CategoryId);
        entity.HasIndex(x => x.Status);

        // Query Filters
        entity.HasQueryFilter(x => !x.IsDelete);
    }
}
