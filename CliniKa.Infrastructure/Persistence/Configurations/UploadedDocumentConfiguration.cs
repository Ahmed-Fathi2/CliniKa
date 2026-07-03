using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class UploadedDocumentConfiguration : IEntityTypeConfiguration<UploadedDocument>
    {
        public void Configure(EntityTypeBuilder<UploadedDocument> builder)
        {
            builder.ToTable("UploadedDocuments");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.DocumentType)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(d => d.FilePath)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(d => d.UploadedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired();

            // Relationship
            builder.HasOne(d => d.ClinicalSession)
                .WithMany(s => s.UploadedDocuments)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
