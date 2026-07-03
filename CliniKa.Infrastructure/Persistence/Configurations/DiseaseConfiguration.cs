using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class DiseaseConfiguration : IEntityTypeConfiguration<Disease>
    {
        public void Configure(EntityTypeBuilder<Disease> builder)
        {
            builder.ToTable("Diseases");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.DiseaseCode)
                .HasMaxLength(15)
                .IsRequired();

            builder.HasIndex(d => d.DiseaseCode)
                .IsUnique();

            builder.Property(d => d.DiseaseName)
                .HasMaxLength(150)
                .IsRequired();
        }
    }
}
