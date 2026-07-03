using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class ActiveMedicationConfiguration : IEntityTypeConfiguration<ActiveMedication>
    {
        public void Configure(EntityTypeBuilder<ActiveMedication> builder)
        {
            builder.ToTable("ActiveMedications");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.DrugName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(m => m.Dosage)
                .HasMaxLength(50);

            builder.Property(m => m.Frequency)
                .HasMaxLength(50);

            builder.Property(m => m.StartDate)
                .HasColumnType("DATE")
                .IsRequired();

            builder.Property(m => m.EndDate)
                .HasColumnType("DATE");

            builder.Property(m => m.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            // Index: Active medications for patient
            builder.HasIndex(m => m.PatientId)
                .HasDatabaseName("idx_active_meds")
                .HasFilter("[IsActive] = 1");

            // Relationships
            builder.HasOne(m => m.Patient)
                .WithMany(p => p.ActiveMedications)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.SourceSession)
                .WithMany(s => s.ActiveMedications)
                .HasForeignKey(m => m.SourceSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
