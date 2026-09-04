using FyM.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FyM.Users.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).HasMaxLength(80).IsRequired();
        builder.Property(a => a.EntityName).HasMaxLength(80).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(a => a.IpAddress).HasMaxLength(45);

        builder.HasIndex(a => a.TimestampUtc).IsDescending().HasDatabaseName("IX_AuditLogs_TimestampUtc");
        builder.HasIndex(a => a.UserId).HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
