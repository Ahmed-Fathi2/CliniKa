using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class PatientDiseaseConfiguration : IEntityTypeConfiguration<PatientDisease>
    {
        public void Configure(EntityTypeBuilder<PatientDisease> builder)
        {
            builder.ToTable("PatientDiseases");

            builder.HasKey(pd => pd.Id);

            builder.Property(pd => pd.RawDiseaseName)
                .HasMaxLength(200);

            builder.Property(pd => pd.DiagnosedDate)
                .HasColumnType("DATE")
                .HasDefaultValueSql("CAST(GETDATE() AS DATE)")
                .IsRequired();

            // Index
            builder.HasIndex(pd => pd.PatientId)
                .HasDatabaseName("idx_patient_diseases");

            // Relationships
            builder.HasOne(pd => pd.Patient)
                .WithMany(p => p.PatientDiseases)
                .HasForeignKey(pd => pd.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pd => pd.Disease)
                .WithMany(d => d.PatientDiseases)
                .HasForeignKey(pd => pd.DiseaseId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasOne(pd => pd.SourceSession)
                .WithMany(s => s.PatientDiseases)
                .HasForeignKey(pd => pd.SourceSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
