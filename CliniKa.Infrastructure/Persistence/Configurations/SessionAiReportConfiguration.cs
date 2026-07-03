using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class SessionAiReportConfiguration : IEntityTypeConfiguration<SessionAiReport>
    {
        public void Configure(EntityTypeBuilder<SessionAiReport> builder)
        {
            builder.ToTable("SessionAIReports");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.GeneratedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired();

            // 1-to-1 relationship with ClinicalSession
            builder.HasOne(r => r.ClinicalSession)
                .WithOne(s => s.SessionAiReport)
                .HasForeignKey<SessionAiReport>(r => r.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
