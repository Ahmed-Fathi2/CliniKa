using CliniKa.Application;
using CliniKa.Application.Abstractions.Authentication;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs;
using CliniKa.Application.DTOs.FormFile;
using CliniKa.Application.DTOs.Langflow;
using CliniKa.Application.DTOs.Sessions;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using Microsoft.AspNetCore.Http;

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

            // 1. Upload audio file to local storage (for our records)
            var uploadAudioRequest = new UploadFileRequest(audioFile);
            var audioUploadResult = await _fileService.UploadFileAsync(uploadAudioRequest, baseUrl, schema, host);
            if (!audioUploadResult.IsSuccess || string.IsNullOrEmpty(audioUploadResult.Data))
            {
                return GeneralResult<string>.Failure("Failed to upload audio file.");
            }

            // 2. Upload documents to local storage (for our records)
            var uploadedDocs = new List<(IFormFile File, string SavedUrl)>();
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
                }
            }

            // 3. Register uploaded documents in the database
            RegisterUploadedDocuments(session, uploadedDocs);

            // 4. Update session status to "Processing"
            session.Status = "Processing";

            await _unitOfWork.SaveChangesAsync();

            // 5. Copy IFormFile content to MemoryStreams so they survive after the HTTP request ends.
            //    The original IFormFile streams get disposed when the request completes,
            //    so we must buffer them before firing the background task.
            var audioBytes = await ReadFormFileToBytes(audioFile);
            var audioFileName = audioFile.FileName;
            var audioContentType = audioFile.ContentType;

            var docBytesList = new List<(byte[] Bytes, string FileName, string ContentType)>();
            if (documents != null)
            {
                foreach (var doc in documents)
                {
                    if (doc.Length == 0) continue;
                    var bytes = await ReadFormFileToBytes(doc);
                    docBytesList.Add((bytes, doc.FileName, doc.ContentType));
                }
            }

            // 6. Trigger the Activepieces flow in the background (fire-and-forget).
            //    The flow returns 200 OK immediately and calls back when done.
            _ = Task.Run(async () =>
            {
                try
                {
                    // Create in-memory IFormFile wrappers from the buffered bytes
                    var bufferedAudio = CreateFormFileFromBytes(audioBytes, audioFileName, audioContentType);
                    var bufferedDocs = new List<IFormFile>();
                    foreach (var (bytes, name, ct) in docBytesList)
                    {
                        bufferedDocs.Add(CreateFormFileFromBytes(bytes, name, ct));
                    }

                    var triggerResult = await _langFlowService.TriggerDiagnosisFlowAsync(
                        sessionId, bufferedAudio, bufferedDocs);

                    if (triggerResult.IsSuccess)
                    {
                        Console.WriteLine($"[SessionService] Activepieces flow triggered successfully for Session {sessionId}.");
                    }
                    else
                    {
                        Console.WriteLine($"[SessionService] Activepieces flow failed for Session {sessionId}: {triggerResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SessionService] Background trigger exception for Session {sessionId}: {ex.Message}");
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

        public async Task<GeneralResult<WorkflowSessionPayloadDto>> GetWorkflowPayloadAsync(int sessionId)
        {
            var session = await _unitOfWork.ClinicalSessions.GetAsync(s => s.Id == sessionId,false);

            if (session == null)
                return GeneralResult<WorkflowSessionPayloadDto>.Failure("Session not found");

            var patient = await _unitOfWork.Patient.GetAsync(p => p.Id == session.PatientId, trackChanges: false);

            var prescribedMedications = await _unitOfWork.PrescribedMedications.GetAllAsync(
                    filter: m => m.SessionId == sessionId,
                    sortBy: null,
                    isDescending: false,
                    trackChanges: false);

            var newMedicationsDto = prescribedMedications.Select(m => m.DrugName).FirstOrDefault(); //string.Join(", ", prescribedMedications.Select(m => m.DrugName));

            var activeMedications = await _unitOfWork.ActiveMedications.GetAllAsync(
                filter: m => m.PatientId == session.PatientId && (m.EndDate == null || m.EndDate.Value.Date >= DateTime.UtcNow.Date),
                sortBy: null,
                isDescending: false,
                trackChanges: false);

            var currentMedicationsDto = activeMedications.Select(m => m.DrugName).ToList();

            var aiReport = await _unitOfWork.SessionAIReports.GetAsync(r => r.SessionId == sessionId, trackChanges: false);

            var payload = new WorkflowSessionPayloadDto
            {
                SessionId = session.Id,
                PatientId = session.PatientId,
                PatientName = patient?.FullName,
                AudioTranscript = session.AudioTranscript,
                //NewMedications = newMedicationsDto,
                CurrentMedications = currentMedicationsDto,
                PossibleDiagnoses = aiReport?.PossibleDiagnoses
            };

            return GeneralResult<WorkflowSessionPayloadDto>.Success(payload);
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

        private static async Task<byte[]> ReadFormFileToBytes(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return ms.ToArray();
        }

        private static IFormFile CreateFormFileFromBytes(byte[] bytes, string fileName, string contentType)
        {
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }
    }
}
