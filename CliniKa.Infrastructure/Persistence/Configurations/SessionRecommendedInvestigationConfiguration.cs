using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class SessionRecommendedInvestigationConfiguration : IEntityTypeConfiguration<SessionRecommendedInvestigation>
    {
        public void Configure(EntityTypeBuilder<SessionRecommendedInvestigation> builder)
        {
            builder.ToTable("SessionRecommendedInvestigations");

            builder.HasKey(ri => ri.Id);

            builder.Property(ri => ri.Investigation)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(ri => ri.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired();

            // Indexes for ultra-fast query performance
            builder.HasIndex(ri => ri.SessionId)
                .HasDatabaseName("idx_recommended_investigations_session");

            builder.HasIndex(ri => new { ri.SessionId, ri.Order })
                .HasDatabaseName("idx_recommended_investigations_session_order");

            // Relationship
            builder.HasOne(ri => ri.ClinicalSession)
                .WithMany(s => s.RecommendedInvestigations)
                .HasForeignKey(ri => ri.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
