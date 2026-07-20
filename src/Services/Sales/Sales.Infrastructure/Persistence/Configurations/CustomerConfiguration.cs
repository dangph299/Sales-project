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
        entity.HasIndex(x => x.CustomerCode).IsUnique();
        entity.HasIndex(x => x.NormalizedPhone).IsUnique().HasFilter("\"NormalizedPhone\" IS NOT NULL");
        entity.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        entity.HasIndex(x => x.Phone);
        entity.HasIndex(x => x.ReversedPhone);
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
