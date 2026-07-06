using CliniKa.Domain.Entities;
using CliniKa.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.ToTable("Patients");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.FullName)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(p => p.FullName)
                .HasDatabaseName("idx_patients_name");

            builder.Property(p => p.DateOfBirth)
                .HasColumnType("DATE")
                .IsRequired();

            builder.Property(p => p.Gender)
                .HasMaxLength(10);

            builder.Property(p => p.BloodType)
                .HasMaxLength(5);

            builder.Property(p => p.NationalId)
                .HasMaxLength(20);

            builder.HasIndex(p => p.NationalId)
                .IsUnique()
                .HasDatabaseName("idx_patients_national_id");

            builder.Property(p => p.Phone)
                .HasMaxLength(20);

            builder.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Identity link: 1-to-1 with ApplicationUser
            builder.Property(p => p.UserId)
                .IsRequired();

            builder.HasIndex(p => p.UserId)
                .IsUnique();

            builder.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
