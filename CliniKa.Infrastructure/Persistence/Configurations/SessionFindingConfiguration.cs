using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class SessionFindingConfiguration : IEntityTypeConfiguration<SessionFinding>
    {
        public void Configure(EntityTypeBuilder<SessionFinding> builder)
        {
            builder.ToTable("SessionFindings");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.FindingType)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(f => f.Description)
                .IsRequired();

            builder.Property(f => f.Severity)
                .HasMaxLength(20);

            builder.Property(f => f.DoctorAction)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            builder.Property(f => f.LoggedAt)
                .HasDefaultValueSql("GETDATE()");

            // Index
            builder.HasIndex(f => f.SessionId)
                .HasDatabaseName("idx_findings_session");

            // Relationship
            builder.HasOne(f => f.ClinicalSession)
                .WithMany(s => s.SessionFindings)
                .HasForeignKey(f => f.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
