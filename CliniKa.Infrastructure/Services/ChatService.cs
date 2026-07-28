using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Chat;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using System.Text;
using System.Text.Json;

namespace CliniKa.Infrastructure.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILangFlowService _langFlowService;

        public ChatService(IUnitOfWork unitOfWork, ILangFlowService langFlowService)
        {
            _unitOfWork = unitOfWork;
            _langFlowService = langFlowService;
        }

        /// <inheritdoc />
        public async Task<GeneralResult<SendMessageResponseDto>> SendMessageAsync(int patientId, int? chatSessionId, string message)
        {
            ChatSession chatSession;
            List<ChatMessageDto> messages;

            if (!chatSessionId.HasValue || chatSessionId.Value == 0)
            {
                // 1. Fetch patient with all medical data
                var patient = await _unitOfWork.Patient.GetPatientWithIncludesAsync(patientId);
                if (patient == null)
                {
                    return GeneralResult<SendMessageResponseDto>.NotFound("Patient not found.");
                }

                // 2. Build system prompt from patient data
                var systemPrompt = BuildSystemPrompt(patient);

                // 3. Create title directly from initial user message
                string initialTitle = message.Length > 50 ? message.Substring(0, 50) + "..." : message;

                chatSession = new ChatSession
                {
                    PatientId = patientId,
                    Title = initialTitle,
                    SystemPrompt = systemPrompt,
                    MessagesJson = "[]",
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow,
                    IsActive = true
                };

                _unitOfWork.ChatSessions.Add(chatSession);
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

            // Build payload for AI flow (Sanitize history to guarantee strict user/assistant role alternation)
            var flowPayload = new ChatFlowPayloadDto
            {
                SystemPrompt = chatSession.SystemPrompt,
                NewQuestion = message,
                Messages = SanitizeChatHistoryForFlow(messages)
            };

            // Append the new user message to local history array
            var userMessage = new ChatMessageDto
            {
                Role = "user",
                Content = message,
                Timestamp = DateTime.UtcNow
            };
            messages.Add(userMessage);

            // Call ActivePieces chatbot flow
            var flowResult = await _langFlowService.TriggerChatbotFlowAsync(flowPayload);
            if (!flowResult.IsSuccess || string.IsNullOrEmpty(flowResult.Data))
            {
                return GeneralResult<SendMessageResponseDto>.Failure(flowResult.Message ?? "Failed to get a response from the AI service.");
            }

            // Append AI response to history
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

        /// <inheritdoc />
        public async Task<GeneralResult<List<ChatMessageDto>>> GetChatHistoryAsync(int chatSessionId)
        {
            var chatSession = await _unitOfWork.ChatSessions.GetAsync(
                c => c.Id == chatSessionId,
                trackChanges: true);

            if (chatSession == null)
            {
                return GeneralResult<List<ChatMessageDto>>.NotFound("Chat session not found.");
            }

            // Refresh SystemPrompt with current patient medical profile whenever a chat session is opened
            var patient = await _unitOfWork.Patient.GetPatientWithIncludesAsync(chatSession.PatientId);
            if (patient != null)
            {
                chatSession.SystemPrompt = BuildSystemPrompt(patient);
                await _unitOfWork.SaveChangesAsync();
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

        #region Private Helpers

        /// <summary>
        /// Builds a comprehensive system prompt from patient medical data.
        /// This prompt is sent to the LLM with every message so it has full patient context.
        /// </summary>
        private static string BuildSystemPrompt(Patient patient)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a medical AI assistant. You have access to the following patient's medical profile.");
            sb.AppendLine("Answer health questions considering this medical history.");
            sb.AppendLine("Do NOT prescribe or change medications. Advise the patient to consult their doctor for any changes.");
            sb.AppendLine("Be empathetic, clear, and use simple language the patient can understand.");
            sb.AppendLine();

            // Patient demographics
            sb.AppendLine("=== PATIENT PROFILE ===");
            sb.AppendLine($"Name: {patient.FullName}");
            if (!string.IsNullOrEmpty(patient.Gender))
                sb.AppendLine($"Gender: {patient.Gender}");
            if (patient.DateOfBirth.HasValue && patient.DateOfBirth.Value != default)
            {
                var age = DateTime.UtcNow.Year - patient.DateOfBirth.Value.Year;
                if (DateTime.UtcNow.DayOfYear < patient.DateOfBirth.Value.DayOfYear) age--;
                sb.AppendLine($"Age: {age} years");
            }
            if (!string.IsNullOrEmpty(patient.BloodType))
                sb.AppendLine($"Blood Type: {patient.BloodType}");
            sb.AppendLine();

            // Active Medications
            sb.AppendLine("=== ACTIVE MEDICATIONS ===");
            var activeMeds = patient.ActiveMedications
                .Where(m => m.IsActive)
                .ToList();

            if (activeMeds.Any())
            {
                foreach (var med in activeMeds)
                {
                    var details = new List<string> { med.DrugName };
                    if (!string.IsNullOrEmpty(med.Dosage)) details.Add(med.Dosage);
                    if (!string.IsNullOrEmpty(med.Frequency)) details.Add(med.Frequency);
                    details.Add($"since {med.StartDate:yyyy-MM-dd}");
                    sb.AppendLine($"- {string.Join(" | ", details)}");
                }
            }
            else
            {
                sb.AppendLine("- No active medications on record.");
            }
            sb.AppendLine();

            // Chronic Diseases
            sb.AppendLine("=== CHRONIC DISEASES ===");
            if (patient.PatientDiseases.Any())
            {
                foreach (var pd in patient.PatientDiseases)
                {
                    var diseaseName = pd.Disease?.DiseaseName ?? pd.RawDiseaseName ?? "Unknown";
                    sb.AppendLine($"- {diseaseName} (diagnosed {pd.DiagnosedDate:yyyy-MM-dd})");
                }
            }
            else
            {
                sb.AppendLine("- No chronic diseases on record.");
            }
            sb.AppendLine();

            // Allergies
            sb.AppendLine("=== KNOWN ALLERGIES ===");
            if (patient.PatientAllergies.Any())
            {
                foreach (var pa in patient.PatientAllergies)
                {
                    var allergenName = pa.Allergy?.AllergenName ?? pa.RawAllergenName ?? "Unknown";
                    var severity = !string.IsNullOrEmpty(pa.Severity) ? $" (Severity: {pa.Severity})" : "";
                    sb.AppendLine($"- {allergenName}{severity}");
                }
            }
            else
            {
                sb.AppendLine("- No known allergies on record.");
            }

            return sb.ToString();
        }

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

        /// <summary>
        /// Sanitizes history sent to HuggingFace/Transformers LLM endpoints to enforce strict role alternation (user/assistant).
        /// Trims any incomplete trailing user messages so that newQuestion ('user') can be cleanly appended by the Python server.
        /// </summary>
        private static List<ChatMessageDto> SanitizeChatHistoryForFlow(List<ChatMessageDto> rawMessages)
        {
            if (rawMessages == null || !rawMessages.Any())
                return new List<ChatMessageDto>();

            var cleanList = new List<ChatMessageDto>();
            string expectedRole = "user";

            foreach (var msg in rawMessages)
            {
                if (string.Equals(msg.Role, expectedRole, StringComparison.OrdinalIgnoreCase))
                {
                    cleanList.Add(msg);
                    expectedRole = expectedRole.Equals("user", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
                }
            }

            // Ensure history payload ends with 'assistant' so Python endpoint can append 'newQuestion' ('user') without back-to-back user roles
            if (cleanList.Any() && cleanList.Last().Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                cleanList.RemoveAt(cleanList.Count - 1);
            }

            return cleanList;
        }

        #endregion
    }
}
