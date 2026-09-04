using FyM.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FyM.Users.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(88).IsRequired();
        builder.Property(t => t.CreatedByIp).HasMaxLength(45);
        builder.Property(t => t.RevokedByIp).HasMaxLength(45);
        builder.Property(t => t.UserAgent).HasMaxLength(256);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(88);

        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("IX_RefreshTokens_TokenHash");
        builder.HasIndex(t => new { t.UserId, t.ExpiresAtUtc }).HasDatabaseName("IX_RefreshTokens_UserId_ExpiresAtUtc");

        builder.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
