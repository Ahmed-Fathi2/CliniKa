using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class SessionDifferentialDiagnosisConfiguration : IEntityTypeConfiguration<SessionDifferentialDiagnosis>
    {
        public void Configure(EntityTypeBuilder<SessionDifferentialDiagnosis> builder)
        {
            builder.ToTable("SessionDifferentialDiagnoses");

            builder.HasKey(dd => dd.Id);

            builder.Property(dd => dd.Diagnosis)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(dd => dd.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired();

            // Index
            builder.HasIndex(dd => dd.SessionId)
                .HasDatabaseName("idx_differential_diagnoses_session");

            builder.HasIndex(dd => new { dd.SessionId, dd.Rank })
                .HasDatabaseName("idx_differential_diagnoses_session_rank");

            // Relationship
            builder.HasOne(dd => dd.ClinicalSession)
                .WithMany(s => s.DifferentialDiagnoses)
                .HasForeignKey(dd => dd.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
