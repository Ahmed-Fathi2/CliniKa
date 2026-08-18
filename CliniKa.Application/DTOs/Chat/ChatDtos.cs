using System.Text.Json.Serialization;

namespace CliniKa.Application.DTOs.Chat
{
    public class SendMessageRequestDto
    {
        public int PatientId { get; set; }
        public int? ChatSessionId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SendMessageResponseDto
    {
        public int ChatSessionId { get; set; }
        public string Reply { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ChatMessageDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Lightweight DTO for sidebar — listing previous conversations.
    /// </summary>
    public class ChatSessionSummaryDto
    {
        public int ChatSessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public int MessageCount { get; set; }
    }
}
