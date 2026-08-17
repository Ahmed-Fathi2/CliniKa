using CliniKa.Domain.Entities;
using CliniKa.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            builder.ToTable("Doctors");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.FullName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(d => d.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Identity link: 1-to-1 with ApplicationUser
            builder.Property(d => d.UserId)
                .IsRequired();

            builder.HasIndex(d => d.UserId)
                .IsUnique();

            builder.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
