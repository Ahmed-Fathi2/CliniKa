using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using CliniKa.Application;
using CliniKa.Application.Abstractions.Authentication;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.FormFile;
using CliniKa.Application.DTOs.Sessions;
using CliniKa.Application.DTOs.Langflow;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;

namespace CliniKa.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly ILangFlowService _langFlowService;
        private readonly IFileService _fileService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public SessionService(
            ILangFlowService langFlowService,
            IFileService fileService,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _langFlowService = langFlowService;
            _fileService = fileService;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        /// <inheritdoc />
        public async Task<GeneralResult<int>> CreateSessionAsync(CreateClinicalSessionDto dto)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return GeneralResult<int>.Failure("User is not authenticated.");
            }

            var doctor = await _unitOfWork.Doctors.GetAsync(d => d.UserId == userId);
            if (doctor == null)
            {
                return GeneralResult<int>.NotFound("Doctor profile not found for the authenticated user.");
            }

            var session = new ClinicalSession
            {
                PatientId = dto.PatientId,
                DoctorId = doctor.Id,
                VisitDate = DateTime.UtcNow,
                Status = "In Progress"
            };

            _unitOfWork.ClinicalSessions.Add(session);
            await _unitOfWork.SaveChangesAsync();

            return GeneralResult<int>.Success(session.Id, "Clinical session started successfully.");
        }

        /// <inheritdoc />
        public async Task<GeneralResult<string>> DiagnoseSessionAsync(
            int sessionId,
            IFormFile audioFile,
            List<IFormFile>? documents,
            string schema,
            string host,
            string baseUrl)
        {
            var session = await _unitOfWork.ClinicalSessions.GetAsync(
                s => s.Id == sessionId,
                trackChanges: true,
                s => s.UploadedDocuments,
                s => s.SessionAiReport);

            if (session == null)
            {
                return GeneralResult<string>.NotFound("Clinical Session not found.");
            }

            // 1. Upload audio file
            var uploadAudioRequest = new UploadFileRequest(audioFile);
            var audioUploadResult = await _fileService.UploadFileAsync(uploadAudioRequest, baseUrl, schema, host);
            if (!audioUploadResult.IsSuccess || string.IsNullOrEmpty(audioUploadResult.Data))
            {
                return GeneralResult<string>.Failure("Failed to upload audio file.");
            }
            var audioUrl = audioUploadResult.Data;

            // Resolve local path for audio file
            var audioFileName = Path.GetFileName(new Uri(audioUrl).LocalPath);
            var localAudioPath = Path.Combine(baseUrl, "uploads", audioFileName);

            // 2. Upload documents
            var uploadedDocs = new List<(IFormFile File, string SavedUrl)>();
            var localDocPaths = new List<string>();
            if (documents != null)
            {
                foreach (var doc in documents)
                {
                    if (doc.Length == 0) continue;
                    var uploadRequest = new UploadFileRequest(doc);
                    var docResult = await _fileService.UploadFileAsync(uploadRequest, baseUrl, schema, host);
                    if (!docResult.IsSuccess || string.IsNullOrEmpty(docResult.Data))
                    {
                        return GeneralResult<string>.Failure("Failed to upload one or more documents.");
                    }
                    uploadedDocs.Add((doc, docResult.Data));
                    var docFileName = Path.GetFileName(new Uri(docResult.Data).LocalPath);
                    localDocPaths.Add(Path.Combine(baseUrl, "uploads", docFileName));
                }
            }

            // 3. Register uploaded documents in the database
            RegisterUploadedDocuments(session, uploadedDocs);

            // 4. Update session status to "Processing"
            session.Status = "Processing";

            await _unitOfWork.SaveChangesAsync();

            // 5. Trigger the Activepieces flow in the background (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _langFlowService.TriggerDiagnosisFlowAsync(sessionId, localAudioPath, localDocPaths);
                }
                catch (Exception)
                {
                    // Fail silently in background
                }
            });

            return GeneralResult<string>.Success("Processing started", "Diagnosis has been queued for processing.");
        }

        /// <inheritdoc />
        public async Task<GeneralResult<string>> SaveDiagnosisCallbackResultAsync(
            int sessionId,
            LangFlowDiagnosisResult result)
        {
            var session = await _unitOfWork.ClinicalSessions.GetAsync(
                s => s.Id == sessionId,
                trackChanges: true,
                s => s.SessionAiReport);

            if (session == null)
            {
                return GeneralResult<string>.NotFound("Clinical Session not found.");
            }

            session.AudioTranscript = result.Body?.Transcript ?? string.Empty;
            session.ExtractedSymptoms = result.Body?.ChiefComplaint ?? string.Empty;
            session.Status = "Completed";

            var aiReport = session.SessionAiReport;
            if (aiReport == null)
            {
                aiReport = new SessionAiReport
                {
                    SessionId = session.Id,
                    GeneratedAt = DateTime.UtcNow
                };
                session.SessionAiReport = aiReport;
            }

            aiReport.PatientSummary = result.Body?.HistoryOfPresentIllness ?? string.Empty;
            
            aiReport.PossibleDiagnoses = result.Body?.PossibleDiagnosis ?? 
                (result.Body?.Top5DifferentialDiagnoses != null ? string.Join(", ", result.Body.Top5DifferentialDiagnoses) : string.Empty);
            
            var investigations = new List<string>();
            if (!string.IsNullOrEmpty(result.Body?.CompleteFindingsFromAllMedicalImages))
            {
                investigations.Add(result.Body.CompleteFindingsFromAllMedicalImages);
            }
            if (result.Body?.RecommendedInvestigations != null)
            {
                investigations.AddRange(result.Body.RecommendedInvestigations);
            }
            aiReport.SuggestedInvestigations = string.Join("; ", investigations);
            
            aiReport.ClinicalAlerts = result.Body?.ImmediateManagement ?? string.Empty;

            await _unitOfWork.SaveChangesAsync();

            // TODO: Broadcast the results to frontend clients using SignalR Hub (e.g. SessionHub)

            return GeneralResult<string>.Success("Callback result saved successfully.", "Success");
        }

        private void RegisterUploadedDocuments(
            ClinicalSession session,
            List<(IFormFile File, string SavedUrl)> uploadedDocs)
        {
            foreach (var doc in uploadedDocs)
            {
                var docType = Path.GetExtension(doc.File.FileName).ToLower() == ".pdf"
                    ? "PDF"
                    : "Image";

                var uploadedDoc = new UploadedDocument
                {
                    SessionId = session.Id,
                    DocumentType = docType,
                    FilePath = doc.SavedUrl,
                    UploadedAt = DateTime.UtcNow
                };

                session.UploadedDocuments.Add(uploadedDoc);
            }
        }
    }
}
