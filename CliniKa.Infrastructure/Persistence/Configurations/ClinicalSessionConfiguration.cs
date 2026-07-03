using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class ClinicalSessionConfiguration : IEntityTypeConfiguration<ClinicalSession>
    {
        public void Configure(EntityTypeBuilder<ClinicalSession> builder)
        {
            builder.ToTable("ClinicalSessions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.VisitDate)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired();

            builder.Property(s => s.FinalDiagnosis)
                .HasMaxLength(500);

            builder.Property(s => s.Status)
                .HasMaxLength(20)
                .HasDefaultValue("In Progress")
                .IsRequired();

            // Index
            builder.HasIndex(s => new { s.PatientId, s.VisitDate })
                .IsDescending(false, true)
                .HasDatabaseName("idx_sessions_patient_date");

            // Relationships
            builder.HasOne(s => s.Patient)
                .WithMany(p => p.ClinicalSessions)
                .HasForeignKey(s => s.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.Doctor)
                .WithMany(d => d.ClinicalSessions)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
