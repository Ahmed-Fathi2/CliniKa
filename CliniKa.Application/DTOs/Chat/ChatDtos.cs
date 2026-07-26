using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CliniKa.Application.DTOs.Chat
{
    public class StartChatRequestDto
    {
        public int PatientId { get; set; }
    }

    public class StartChatResponseDto
    {
        public int ChatSessionId { get; set; }
        public string WelcomeMessage { get; set; } = string.Empty;
    }

    public class SendMessageRequestDto
    {
        public int ChatSessionId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SendMessageResponseDto
    {
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

    /// <summary>
    /// Payload sent to ActivePieces chatbot flow.
    /// </summary>
    public class ChatFlowPayloadDto
    {
        [JsonPropertyName("systemPrompt")]
        public string SystemPrompt { get; set; } = string.Empty;
        
        [JsonPropertyName("newQuestion")]
        public string NewQuestion { get; set; } = string.Empty;
        
        [JsonPropertyName("messages")]
        public List<ChatMessageDto> Messages { get; set; } = new();
    }
}
