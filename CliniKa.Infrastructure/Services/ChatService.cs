using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Chat;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace CliniKa.Infrastructure.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILangFlowService _langFlowService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChatService(IUnitOfWork unitOfWork, ILangFlowService langFlowService, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _langFlowService = langFlowService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public async Task<GeneralResult<List<ChatMessageDto>>> GetChatHistoryAsync(int chatSessionId)
        {
            var chatSession = await _unitOfWork.ChatSessions.GetAsync(
                c => c.Id == chatSessionId,
                trackChanges: false);

            if (chatSession == null)
            {
                return GeneralResult<List<ChatMessageDto>>.NotFound("Chat session not found.");
            }

            var messages = DeserializeMessages(chatSession.MessagesJson);
            return GeneralResult<List<ChatMessageDto>>.Success(messages, "Chat history retrieved successfully.");
        }

        /// <inheritdoc />
        public async Task<GeneralResult<List<ChatSessionSummaryDto>>> GetPatientChatsAsync(int patientId)
        {
            // Verify patient exists
            var patient = await _unitOfWork.Patient.GetByIdAsync(patientId);
            if (patient == null)
            {
                return GeneralResult<List<ChatSessionSummaryDto>>.NotFound("Patient not found.");
            }

            var chatSessions = await _unitOfWork.ChatSessions.GetAllAsync(
                filter: c => c.PatientId == patientId && c.IsActive,
                sortBy: "LastMessageAt",
                isDescending: true,
                trackChanges: false);

            var summaries = chatSessions.Select(c =>
            {
                var messages = DeserializeMessages(c.MessagesJson);
                return new ChatSessionSummaryDto
                {
                    ChatSessionId = c.Id,
                    Title = c.Title,
                    LastMessageAt = c.LastMessageAt,
                    MessageCount = messages.Count
                };
            }).ToList();

            return GeneralResult<List<ChatSessionSummaryDto>>.Success(summaries, "Patient chats retrieved successfully.");
        }

        /// <inheritdoc />
        public async Task<GeneralResult<SendMessageResponseDto>> SendMessageViaAgentAsync(int patientId, int? chatSessionId, string message)
        {
            ChatSession chatSession;
            List<ChatMessageDto> messages;

            if (!chatSessionId.HasValue || chatSessionId.Value == 0)
            {
                // Verify patient exists
                var patient = await _unitOfWork.Patient.GetByIdAsync(patientId);
                if (patient == null)
                {
                    return GeneralResult<SendMessageResponseDto>.NotFound("Patient not found.");
                }

                // Create title from initial user message
                string initialTitle = message.Length > 50 ? message.Substring(0, 50) + "..." : message;

                chatSession = new ChatSession
                {
                    PatientId = patientId,
                    Title = initialTitle,
                    MessagesJson = "[]",
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow,
                    IsActive = true
                };

                _unitOfWork.ChatSessions.Add(chatSession);
                // Save now to generate the DB Id — used as Langflow session_id
                await _unitOfWork.SaveChangesAsync();
                messages = new List<ChatMessageDto>();
            }
            else
            {
                // Load existing chat session
                var existingSession = await _unitOfWork.ChatSessions.GetAsync(
                    c => c.Id == chatSessionId.Value && c.IsActive,
                    trackChanges: true);

                if (existingSession == null)
                {
                    return GeneralResult<SendMessageResponseDto>.NotFound("Chat session not found or is no longer active.");
                }

                chatSession = existingSession;
                messages = DeserializeMessages(chatSession.MessagesJson);
            }

            // Extract JWT Bearer token from the current HTTP request
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
            var jwtToken = string.Empty;
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                jwtToken = authHeader.Substring("Bearer ".Length).Trim();
            }

            // Use the DB-generated ChatSession.Id as the Langflow session_id for conversation continuity
            var langflowSessionId = $"clinika-chat-{chatSession.Id}";

            // Append user message to local history
            var userMessage = new ChatMessageDto
            {
                Role = "user",
                Content = message,
                Timestamp = DateTime.UtcNow
            };
            messages.Add(userMessage);

            // Call Langflow Agent — agent handles memory and patient context internally
            var flowResult = await _langFlowService.CallLangflowChatbotAgentAsync(message, langflowSessionId, jwtToken);
            if (!flowResult.IsSuccess || string.IsNullOrEmpty(flowResult.Data))
            {
                return GeneralResult<SendMessageResponseDto>.Failure(flowResult.Message ?? "Failed to get a response from the Langflow Agent.");
            }

            // Append AI response to local history
            string aiReply = flowResult.Data;
            var assistantMessage = new ChatMessageDto
            {
                Role = "assistant",
                Content = aiReply,
                Timestamp = DateTime.UtcNow
            };
            messages.Add(assistantMessage);

            // Save updated history to DB
            chatSession.MessagesJson = SerializeMessages(messages);
            chatSession.LastMessageAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return GeneralResult<SendMessageResponseDto>.Success(new SendMessageResponseDto
            {
                ChatSessionId = chatSession.Id,
                Reply = aiReply,
                Timestamp = assistantMessage.Timestamp
            });
        }

        #region Private Helpers
        private static List<ChatMessageDto> DeserializeMessages(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<ChatMessageDto>>(json) ?? new List<ChatMessageDto>();
            }
            catch
            {
                throw new InvalidOperationException("Failed to deserialize chat messages. The stored JSON may be corrupted.");
            }
        }

        private static string SerializeMessages(List<ChatMessageDto> messages)
        {
            return JsonSerializer.Serialize(messages);
        }

        #endregion
    }
}
