using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CliniKa.Domain.Entities;

namespace CliniKa.Infrastructure.Persistence.Configurations
{
    public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
    {
        public void Configure(EntityTypeBuilder<ChatSession> builder)
        {
            builder.ToTable("ChatSessions");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Title)
                .HasMaxLength(200)
                .HasDefaultValue("New Conversation")
                .IsRequired();

            builder.Property(c => c.SystemPrompt)
                .IsRequired();

            builder.Property(c => c.MessagesJson)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValue("[]")
                .IsRequired();

            builder.Property(c => c.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            builder.Property(c => c.LastMessageAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            builder.Property(c => c.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            // Index for listing patient's conversations (sidebar query)
            builder.HasIndex(c => new { c.PatientId, c.LastMessageAt })
                .IsDescending(false, true)
                .HasDatabaseName("idx_chatsessions_patient_lastmsg");

            // Relationship
            builder.HasOne(c => c.Patient)
                .WithMany(p => p.ChatSessions)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
