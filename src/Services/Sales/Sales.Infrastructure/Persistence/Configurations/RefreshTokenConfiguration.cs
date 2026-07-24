using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sales.Infrastructure;

/// <summary>
/// Persistence mapping for refresh tokens.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        // Table
        entity.ToTable("refresh_tokens");

        // Primary Key
        entity.HasKey(x => x.Id);

        // Properties
        entity.Property(x => x.TokenHash).HasMaxLength(64);
        entity.Property(x => x.CreatedByIp).HasMaxLength(45);

        // Indexes
        entity.HasIndex(x => x.TokenHash).IsUnique();
        entity.HasIndex(x => x.UserId);
        entity.HasIndex(x => x.ReplacedByTokenId);
    }
}
