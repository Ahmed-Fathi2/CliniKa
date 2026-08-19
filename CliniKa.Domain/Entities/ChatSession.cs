namespace CliniKa.Domain.Entities
{
    public class ChatSession
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string Title { get; set; } = "New Conversation";
        public string MessagesJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual Patient Patient { get; set; } = null!;
    }
}