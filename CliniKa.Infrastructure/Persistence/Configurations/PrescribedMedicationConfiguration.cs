using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class PrescribedMedicationConfiguration : IEntityTypeConfiguration<PrescribedMedication>
    {
        public void Configure(EntityTypeBuilder<PrescribedMedication> builder)
        {
            builder.ToTable("PrescribedMedications");

            builder.HasKey(pm => pm.Id);

            builder.Property(pm => pm.DrugName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(pm => pm.Dosage)
                .HasMaxLength(50);

            builder.Property(pm => pm.Frequency)
                .HasMaxLength(50);

            builder.Property(pm => pm.Duration)
                .HasMaxLength(50);

            builder.Property(pm => pm.Notes)
                .HasMaxLength(500);

            // Index
            builder.HasIndex(pm => pm.SessionId)
                .HasDatabaseName("idx_prescribed_session");

            // Relationship
            builder.HasOne(pm => pm.ClinicalSession)
                .WithMany(s => s.PrescribedMedications)
                .HasForeignKey(pm => pm.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
