using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain;

namespace Sales.Infrastructure;

public sealed class ColorConfiguration : IEntityTypeConfiguration<Color>
{
    public void Configure(EntityTypeBuilder<Color> entity)
    {
        entity.ToTable("colors");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.ColorCode).IsUnique();
        entity.HasIndex(x => x.Name).IsUnique();
        entity.Property(x => x.ColorCode).HasMaxLength(16);
        entity.Property(x => x.Name).HasMaxLength(100);
        entity.Property(x => x.HexCode).HasMaxLength(7);
        entity.HasData(
            Color.Create(ColorReferenceDataIds.Black, "BLK", "Black", "#000000"),
            Color.Create(ColorReferenceDataIds.White, "WHT", "White", "#FFFFFF"),
            Color.Create(ColorReferenceDataIds.Red, "RED", "Red", "#FF0000"),
            Color.Create(ColorReferenceDataIds.Blue, "BLU", "Blue", "#0000FF"),
            Color.Create(ColorReferenceDataIds.Green, "GRN", "Green", "#008000"));
    }
}
