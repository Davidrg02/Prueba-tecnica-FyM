using FyM.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FyM.Users.Infrastructure.Persistence.Configurations;

public sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        builder.ToTable("DocumentTypes");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code).HasMaxLength(10).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(80).IsRequired();

        builder.HasIndex(d => d.Code).IsUnique().HasDatabaseName("IX_DocumentTypes_Code");
    }
}
