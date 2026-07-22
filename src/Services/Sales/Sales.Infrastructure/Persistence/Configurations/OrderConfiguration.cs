using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Persistence mapping for orders and their lines.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.ToTable("orders");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.CreatedAt);
        entity.HasIndex(x => new { x.Status, x.UpdatedAt, x.Id });
        entity.HasIndex(x => x.OrderCode).IsUnique().HasDatabaseName("IX_orders_OrderCode");
        entity.HasIndex(x => x.CustomerName).HasMethod("gin").HasOperators("gin_trgm_ops");

        // varchar_pattern_ops so that LIKE 'digits%' can use these indexes: the database is created
        // under a non-C collation, where a default B-tree cannot answer a prefix match. Not unique —
        // many orders legitimately share one customer phone number.
        entity.HasIndex(x => x.NormalizedCustomerPhone)
            .HasOperators("varchar_pattern_ops")
            .HasDatabaseName("IX_orders_NormalizedCustomerPhone");
        entity.HasIndex(x => x.ReversedCustomerPhone)
            .HasOperators("varchar_pattern_ops")
            .HasDatabaseName("IX_orders_ReversedCustomerPhone");

        entity.Property(x => x.OrderCode).HasMaxLength(32);
        entity.Property(x => x.CustomerName).HasMaxLength(200);
        entity.Property(x => x.CustomerPhone).HasMaxLength(32);
        entity.Property(x => x.NormalizedCustomerPhone).HasMaxLength(15);
        entity.Property(x => x.ReversedCustomerPhone).HasMaxLength(15);
        entity.Property(x => x.CustomerEmail).HasMaxLength(254);
        entity.Property(x => x.CustomerAddress).HasMaxLength(500);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.Ignore(x => x.Total);
        entity.Ignore(x => x.TotalQuantity);
        entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
