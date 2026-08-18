using CliniKa.Application.DTOs.Chat;
using CliniKa.Application.DTOs.Sessions;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CliniKa.Application.Abstractions.Services
{
    /// <summary>
    /// Service Interface responsible for communicating with Activepieces webhook/flow.
    /// </summary>
    public interface ILangFlowService
    {
        /// <summary>
        /// Sends the audio and medical documents (as actual files) to the Activepieces diagnosis flow.
        /// The flow returns 200 OK immediately and processes in the background,
        /// calling back to the callback endpoint when done.
        /// </summary>
        Task<GeneralResult<string>> TriggerDiagnosisFlowAsync(
            int sessionId,
            IFormFile audioFile,
            List<IFormFile>? documentFiles);

        Task<GeneralResult<object>> TriggerDrugInteractionFlowAsync(WorkflowSessionPayloadDto payload);

        /// <summary>
        /// Calls the Langflow Agent API directly via HTTP POST.
        /// The agent handles chat memory internally via session_id and retrieves
        /// patient medical data using its own tool (get_patient_medical_profile) with the provided JWT.
        /// </summary>
        Task<GeneralResult<string>> CallLangflowChatbotAgentAsync(string userMessage, string sessionId, string jwtToken);
    }
}
