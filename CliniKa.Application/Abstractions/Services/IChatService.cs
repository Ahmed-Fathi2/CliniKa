using System.Collections.Generic;
using System.Threading.Tasks;
using CliniKa.Application.DTOs.Chat;

namespace CliniKa.Application.Abstractions.Services
{
    public interface IChatService
    {
        /// <summary>
        /// Sends a user message via the Langflow Agent flow (agent handles memory and patient context via tools).
        /// Creates the ChatSession in DB if chatSessionId is null or 0.
        /// </summary>
        Task<GeneralResult<SendMessageResponseDto>> SendMessageViaAgentAsync(int patientId, int? chatSessionId, string message);

        /// <summary>
        /// Returns the full message history for a chat session.
        /// Used when the patient navigates to a previous conversation.
        /// </summary>
        Task<GeneralResult<List<ChatMessageDto>>> GetChatHistoryAsync(int chatSessionId);

        /// <summary>
        /// Returns a list of all chat sessions for a patient (for sidebar display).
        /// Ordered by most recent message first.
        /// </summary>
        Task<GeneralResult<List<ChatSessionSummaryDto>>> GetPatientChatsAsync(int patientId);

    }
}
