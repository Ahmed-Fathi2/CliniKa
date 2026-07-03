using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class AllergyConfiguration : IEntityTypeConfiguration<Allergy>
    {
        public void Configure(EntityTypeBuilder<Allergy> builder)
        {
            builder.ToTable("Allergies");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.AllergenName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(a => a.AllergyType)
                .HasMaxLength(50);
        }
    }
}
