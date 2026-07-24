using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain;

namespace Sales.Infrastructure;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> entity)
    {
        // Table
        entity.ToTable("categories");

        // Primary Key
        entity.HasKey(x => x.Id);

        // Properties
        entity.Property(x => x.CategoryCode).HasMaxLength(32);
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.CreatedBy).HasMaxLength(128);
        entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        entity.Property(x => x.DeleteByUser).HasMaxLength(128);
        entity.Property(x => x.DeletedBy).HasMaxLength(128);
        entity.Property(x => x.Version).IsConcurrencyToken();

        // Relationships
        entity.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        // Unique indexes ignore soft-deleted rows so deleted categories do not permanently reserve
        // their business keys behind the global query filter.
        entity.HasIndex(x => x.CategoryCode).IsUnique().HasFilter("NOT \"IsDelete\"");
        entity.HasIndex(x => new { x.Name, x.ParentCategoryId }).IsUnique().HasFilter("NOT \"IsDelete\"");
        entity.HasIndex(x => x.Name).IsUnique().HasFilter("\"ParentCategoryId\" IS NULL AND NOT \"IsDelete\"");
        entity.HasIndex(x => x.ParentCategoryId);
        entity.HasIndex(x => x.Status);

        // Query Filters
        entity.HasQueryFilter(x => !x.IsDelete);

        // Seed Data
        entity.HasData(CategorySeedData.Uncategorized);
    }
}
