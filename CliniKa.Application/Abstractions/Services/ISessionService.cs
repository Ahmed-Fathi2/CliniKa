using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using CliniKa.Application.DTOs.Sessions;
using CliniKa.Application.DTOs.Langflow;

namespace CliniKa.Application.Abstractions.Services
{
    public interface ISessionService
    {
        Task<GeneralResult<int>> CreateSessionAsync(CreateClinicalSessionDto dto);

        Task<GeneralResult<string>> DiagnoseSessionAsync(
            int sessionId,
            IFormFile audioFile,
            List<IFormFile>? documents,
            string schema,
            string host,
            string baseUrl);

        Task<GeneralResult<string>> SaveDiagnosisCallbackResultAsync(
            int sessionId,
            LangFlowDiagnosisResult result);

        Task<GeneralResult<WorkflowSessionPayloadDto>> GetWorkflowPayloadAsync(int sessionId);

        Task<GeneralResult<SessionDiagnosisResultDto>> GetSessionDiagnosisAsync(int sessionId);
    }
}
