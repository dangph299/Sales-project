using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Persistence mapping for customers and lookup-friendly phone data.
/// </summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        entity.ToTable("customers");
        entity.HasKey(x => x.Id);
        entity.HasQueryFilter(x => !x.IsDelete);
        // Exclude soft-deleted rows: a deleted customer must not keep reserving their code or phone
        // number, since nothing in the app can surface the row that causes the conflict.
        entity.HasIndex(x => x.CustomerCode).IsUnique().HasFilter("NOT \"IsDelete\"");
        // One index per phone column, carrying varchar_pattern_ops so the same index enforces
        // uniqueness, answers equality lookups, and serves the LIKE 'digits%' prefix scan the
        // autocomplete relies on. Under this database's non-C collation a default B-tree could not
        // do the last of those, which is why the phone lookups used to seq-scan.
        entity.HasIndex(x => x.NormalizedPhone)
            .IsUnique()
            .HasFilter("\"NormalizedPhone\" IS NOT NULL AND NOT \"IsDelete\"")
            .HasOperators("varchar_pattern_ops");
        entity.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        entity.HasIndex(x => x.Phone);
        entity.HasIndex(x => x.ReversedPhone)
            .HasFilter("\"ReversedPhone\" IS NOT NULL AND NOT \"IsDelete\"")
            .HasOperators("varchar_pattern_ops");
        entity.HasIndex(x => x.Status);
        entity.Property(x => x.CustomerCode).HasMaxLength(32);
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.Phone).HasMaxLength(32);
        entity.Property(x => x.NormalizedPhone).HasMaxLength(15);
        entity.Property(x => x.ReversedPhone).HasMaxLength(15);
        entity.Property(x => x.Email).HasMaxLength(254);
        entity.Property(x => x.Address).HasMaxLength(500);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.CreatedBy).HasMaxLength(128);
        entity.Property(x => x.UpdatedBy).HasMaxLength(128);
        entity.Property(x => x.DeleteByUser).HasMaxLength(128);
        entity.Property(x => x.DeletedBy).HasMaxLength(128);
        entity.Property(x => x.Version).IsConcurrencyToken();
    }
}
