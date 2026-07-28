using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class PatientAllergyConfiguration : IEntityTypeConfiguration<PatientAllergy>
    {
        public void Configure(EntityTypeBuilder<PatientAllergy> builder)
        {
            builder.ToTable("PatientAllergies");

            builder.HasKey(pa => pa.Id);

            builder.Property(pa => pa.RawAllergenName)
                .HasMaxLength(200);

            builder.Property(pa => pa.Severity)
                .HasMaxLength(20);

            // Index
            builder.HasIndex(pa => pa.PatientId)
                .HasDatabaseName("idx_patient_allergies");

            // Relationships
            builder.HasOne(pa => pa.Patient)
                .WithMany(p => p.PatientAllergies)
                .HasForeignKey(pa => pa.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pa => pa.Allergy)
                .WithMany(a => a.PatientAllergies)
                .HasForeignKey(pa => pa.AllergyId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasOne(pa => pa.SourceSession)
                .WithMany(s => s.PatientAllergies)
                .HasForeignKey(pa => pa.SourceSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
