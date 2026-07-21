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

        entity.ToTable("product_variants");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.HasQueryFilter(x => !x.IsDelete);
        entity.HasIndex(x => x.Sku).IsUnique();
        entity.HasIndex(x => new { x.ProductId, x.ColorId, x.SizeId }).IsUnique();
        entity.HasIndex(x => x.Status);
        entity.Property(x => x.Sku).HasMaxLength(96);
        entity.Property(x => x.Price).HasConversion(money).HasColumnType("numeric(18,0)");
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.CreatedBy).HasMaxLength(128);
        entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        entity.Property(x => x.DeleteByUser).HasMaxLength(128);
        entity.Property(x => x.DeletedBy).HasMaxLength(128);
        entity.Property(x => x.Version).IsConcurrencyToken();
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
    }
}
