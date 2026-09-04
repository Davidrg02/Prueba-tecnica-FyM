using FyM.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FyM.Users.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(50).IsRequired();
        builder.Property(r => r.NormalizedName).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(250);

        builder.HasIndex(r => r.NormalizedName).IsUnique().HasDatabaseName("IX_Roles_NormalizedName");

        builder.Property(r => r.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
    }
}
