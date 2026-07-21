using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain;

namespace Sales.Infrastructure;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> entity)
    {
        entity.ToTable("categories");
        entity.HasKey(x => x.Id);
        entity.HasQueryFilter(x => !x.IsDelete);
        // Unique indexes exclude soft-deleted rows. Without the filter a deleted category keeps
        // owning its code and name forever, and because the query filter hides the row the conflict
        // is invisible: creating the same name again fails with a 409 against a record nothing can show.
        entity.HasIndex(x => x.CategoryCode).IsUnique().HasFilter("NOT \"IsDelete\"");
        entity.HasIndex(x => x.ParentCategoryId);
        entity.HasIndex(x => x.Status);
        entity.HasIndex(x => new { x.Name, x.ParentCategoryId }).IsUnique().HasFilter("NOT \"IsDelete\"");
        entity.HasIndex(x => x.Name).IsUnique().HasFilter("\"ParentCategoryId\" IS NULL AND NOT \"IsDelete\"");
        entity.Property(x => x.CategoryCode).HasMaxLength(32);
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.CreatedBy).HasMaxLength(128);
        entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        entity.Property(x => x.DeleteByUser).HasMaxLength(128);
        entity.Property(x => x.DeletedBy).HasMaxLength(128);
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasData(CategorySeedData.Uncategorized);
    }
}
