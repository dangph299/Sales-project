using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// EF Core mapping for <see cref="Customer"/>: table/index/column configuration, including a
/// trigram index on <c>Name</c> for fast substring search and the reversed-phone column for
/// suffix search.
/// </summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        entity.ToTable("customers");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        entity.HasIndex(x => x.Phone);
        entity.HasIndex(x => x.ReversedPhone);
        entity.Property(x => x.Name).HasMaxLength(200);
        entity.Property(x => x.Phone).HasMaxLength(15);
        entity.Property(x => x.ReversedPhone).HasMaxLength(15);
        entity.Property(x => x.Version).IsConcurrencyToken();
    }
}
