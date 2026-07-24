using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain;

namespace Sales.Infrastructure;

public sealed class SizeConfiguration : IEntityTypeConfiguration<Size>
{
    public void Configure(EntityTypeBuilder<Size> entity)
    {
        // Table
        entity.ToTable("sizes");

        // Primary Key
        entity.HasKey(x => x.Id);

        // Properties
        entity.Property(x => x.Code).HasMaxLength(16);
        entity.Property(x => x.Name).HasMaxLength(100);

        // Indexes
        entity.HasIndex(x => x.Code).IsUnique();
        entity.HasIndex(x => x.SortOrder).IsUnique();

        // Seed Data
        entity.HasData(
            Size.Create(SizeReferenceDataIds.ExtraExtraSmall, "XXS", "Extra Extra Small", 10),
            Size.Create(SizeReferenceDataIds.ExtraSmall, "XS", "Extra Small", 20),
            Size.Create(SizeReferenceDataIds.Small, "S", "Small", 30),
            Size.Create(SizeReferenceDataIds.Medium, "M", "Medium", 40),
            Size.Create(SizeReferenceDataIds.Large, "L", "Large", 50),
            Size.Create(SizeReferenceDataIds.ExtraLarge, "XL", "Extra Large", 60),
            Size.Create(SizeReferenceDataIds.ExtraExtraLarge, "XXL", "Extra Extra Large", 70),
            Size.Create(SizeReferenceDataIds.ExtraExtraExtraLarge, "XXXL", "Extra Extra Extra Large", 80));
    }
}
