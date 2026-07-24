using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sales.Domain;

namespace Sales.Infrastructure;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> entity)
    {
        var money = new ValueConverter<Money, decimal>(x => x.Amount, x => Money.Vnd(x));

        // Table
        entity.ToTable("product_variants");

        // Primary Key
        entity.HasKey(x => x.Id);

        // Properties
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.Sku).HasMaxLength(96);
        entity.Property(x => x.Price).HasConversion(money).HasColumnType("numeric(18,0)");
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.CreatedBy).HasMaxLength(128);
        entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        entity.Property(x => x.DeleteByUser).HasMaxLength(128);
        entity.Property(x => x.DeletedBy).HasMaxLength(128);
        entity.Property(x => x.Version).IsConcurrencyToken();

        // Relationships
        entity.HasOne<Product>()
            .WithMany(x => x.Variants)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Color>()
            .WithMany()
            .HasForeignKey(x => x.ColorId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Size>()
            .WithMany()
            .HasForeignKey(x => x.SizeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        // Unique indexes ignore soft-deleted rows so removing a variant does not permanently block
        // re-adding the same colour/size pair or reissuing its SKU.
        entity.HasIndex(x => x.Sku).IsUnique().HasFilter("NOT \"IsDelete\"");
        entity.HasIndex(x => new { x.ProductId, x.ColorId, x.SizeId }).IsUnique().HasFilter("NOT \"IsDelete\"");
        entity.HasIndex(x => x.Status);

        // Query Filters
        entity.HasQueryFilter(x => !x.IsDelete);
    }
}
