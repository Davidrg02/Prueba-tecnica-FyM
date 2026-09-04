using FyM.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FyM.Users.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(p => p.UserId);

        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.MiddleName).HasMaxLength(100);
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.SecondLastName).HasMaxLength(100);
        builder.Property(p => p.DocumentNumber).HasMaxLength(30);
        builder.Property(p => p.PhoneNumber).HasMaxLength(30);
        builder.Property(p => p.JobTitle).HasMaxLength(150);
        builder.Property(p => p.PhotoUrl).HasMaxLength(512);

        builder.HasIndex(p => new { p.DocumentTypeId, p.DocumentNumber })
            .IsUnique()
            .HasFilter("[DocumentTypeId] IS NOT NULL AND [DocumentNumber] IS NOT NULL")
            .HasDatabaseName("IX_UserProfiles_Document");

        builder.HasOne(p => p.DocumentType)
            .WithMany()
            .HasForeignKey(p => p.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
