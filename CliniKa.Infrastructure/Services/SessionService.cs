using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CliniKa.Application;
using CliniKa.Application.Abstraction;
using CliniKa.Application.DTOs;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;

namespace CliniKa.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly ILangFlowService _langFlowService;
        private readonly IUnitOfWork _unitOfWork;

        public SessionService(
            ILangFlowService langFlowService,
            IUnitOfWork unitOfWork)
        {
            _langFlowService = langFlowService;
            _unitOfWork = unitOfWork;
        }

        public async Task<GeneralResult<TranscribeSessionResponse>> TranscribeSessionAudioAsync(int sessionId, Stream audioStream, string fileName)
        {
            try
            {
                // Retrieve clinical session (track changes = true)
                var session = await _unitOfWork.ClinicalSessions.GetAsync(s => s.Id == sessionId, trackChanges: true);
                if (session == null)
                {
                    return GeneralResult<TranscribeSessionResponse>.NotFound("Clinical Session not found.");
                }

                // Call LangFlow STT & clinical entity extraction flow
                var langFlowResult = await _langFlowService.ProcessAudioAsync(audioStream, fileName);
                if (!langFlowResult.IsSuccess || langFlowResult.Data == null)
                {
                    return GeneralResult<TranscribeSessionResponse>.Failure(langFlowResult.Message);
                }

                var data = langFlowResult.Data;

                // Update session details
                session.AudioTranscript = data.AudioTranscript;
                session.ExtractedSymptoms = JsonSerializer.Serialize(data.Symptoms);
                session.MentionedMedications = JsonSerializer.Serialize(data.MentionedMedications);
                session.MentionedHistory = JsonSerializer.Serialize(data.MentionedHistory);

                await _unitOfWork.SaveChangesAsync();

                var response = new TranscribeSessionResponse
                {
                    SessionId = session.Id,
                    AudioTranscript = data.AudioTranscript,
                    ExtractedSymptoms = data.Symptoms,
                    MentionedMedications = data.MentionedMedications,
                    MentionedHistory = data.MentionedHistory
                };

                return GeneralResult<TranscribeSessionResponse>.Success(response, "Audio processed and transcribed successfully.");
            }
            catch (Exception ex)
            {
                return GeneralResult<TranscribeSessionResponse>.Failure($"An error occurred while transcribing audio: {ex.Message}");
            }
        }
    }
}
