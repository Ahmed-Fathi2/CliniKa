using CliniKa.Application;
using CliniKa.Application.Abstractions.Authentication;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs;
using CliniKa.Application.DTOs.FormFile;
using CliniKa.Application.DTOs.Langflow;
using CliniKa.Application.DTOs.Patient;
using CliniKa.Application.DTOs.Sessions;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using Microsoft.AspNetCore.Http;

namespace CliniKa.Application.Services
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

            var newMedicationsDto = prescribedMedications.Select(m => m.DrugName).ToList();

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
                NewMedications = newMedicationsDto,
                CurrentMedications = currentMedicationsDto,
                PossibleDiagnoses = aiReport?.PossibleDiagnoses
            };

            return GeneralResult<WorkflowSessionPayloadDto>.Success(payload);
        }

        public async Task<GeneralResult<SessionDiagnosisResultDto>> GetSessionDiagnosisAsync(int sessionId)
        {
            var session = await _unitOfWork.ClinicalSessions.GetAsync(
                s => s.Id == sessionId,
                trackChanges: false,
                s => s.SessionAiReport,
                s => s.Patient,
                s => s.PrescribedMedications);

            if (session == null)
            {
                return GeneralResult<SessionDiagnosisResultDto>.NotFound($"Clinical Session with ID '{sessionId}' was not found.");
            }

            var prescribedMedDtos = session.PrescribedMedications?.Select(m => new PrescribedMedicationDto
            {
                Id = m.Id,
                DrugName = m.DrugName,
                Dosage = m.Dosage,
                Frequency = m.Frequency,
                Duration = m.Duration,
                Notes = m.Notes
            }).ToList() ?? new List<PrescribedMedicationDto>();

            var resultDto = new SessionDiagnosisResultDto
            {
                SessionId = session.Id,
                PatientId = session.PatientId,
                PatientName = session.Patient != null ? session.Patient.FullName : string.Empty,
                PatientGender = session.Patient?.Gender,
                VisitDate = session.VisitDate,
                Status = session.Status,
                AudioTranscript = session.AudioTranscript,
                ExtractedSymptoms = session.ExtractedSymptoms,
                FinalDiagnosis = session.FinalDiagnosis,
                PatientSummary = session.SessionAiReport?.PatientSummary,
                PossibleDiagnoses = session.SessionAiReport?.PossibleDiagnoses,
                DrugInteractions = session.SessionAiReport?.DrugInteractions,
                Contraindications = session.SessionAiReport?.Contraindications,
                SuggestedInvestigations = session.SessionAiReport?.SuggestedInvestigations,
                ClinicalAlerts = session.SessionAiReport?.ClinicalAlerts,
                GeneratedAt = session.SessionAiReport?.GeneratedAt,
                PrescribedMedications = prescribedMedDtos
            };

            return GeneralResult<SessionDiagnosisResultDto>.Success(resultDto, "Diagnosis result retrieved successfully.");
        }

        /// <inheritdoc />
        public async Task<GeneralResult<List<DoctorSessionReportDto>>> GetDoctorReportsAsync(string? searchTerm = null)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return GeneralResult<List<DoctorSessionReportDto>>.Failure("User is not authenticated.");
            }

            var doctor = await _unitOfWork.Doctors.GetAsync(d => d.UserId == userId);
            if (doctor == null)
            {
                return GeneralResult<List<DoctorSessionReportDto>>.Failure("Doctor profile not found.");
            }

            var sessions = await _unitOfWork.ClinicalSessions.GetAllAsync(
                filter: s => s.DoctorId == doctor.Id,
                sortBy: "VisitDate",
                isDescending: true,
                trackChanges: false,
                s => s.Patient,
                s => s.SessionAiReport,
                s => s.PrescribedMedications);

            var query = sessions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(s =>
                    s.Id.ToString().Contains(term) ||
                    (s.Patient != null && s.Patient.FullName.ToLower().Contains(term)) ||
                    (s.FinalDiagnosis != null && s.FinalDiagnosis.ToLower().Contains(term)) ||
                    (s.SessionAiReport != null && s.SessionAiReport.PossibleDiagnoses != null && s.SessionAiReport.PossibleDiagnoses.ToLower().Contains(term)));
            }

            var resultDtos = query.Select(s => new DoctorSessionReportDto
            {
                SessionId = s.Id,
                PatientId = s.PatientId,
                PatientName = s.Patient != null ? s.Patient.FullName : "Unknown Patient",
                PatientGender = s.Patient?.Gender,
                PatientNationalId = s.Patient?.NationalId,
                VisitDate = s.VisitDate,
                Status = s.Status,
                FinalDiagnosis = s.FinalDiagnosis,
                PossibleDiagnoses = s.SessionAiReport?.PossibleDiagnoses,
                PatientSummary = s.SessionAiReport?.PatientSummary,
                HasAiReport = s.SessionAiReport != null,
                PrescribedMedicationsCount = s.PrescribedMedications?.Count ?? 0
            }).ToList();

            return GeneralResult<List<DoctorSessionReportDto>>.Success(resultDtos, "Doctor reports retrieved successfully.");
        }

        /// <inheritdoc />
        public async Task<GeneralResult<List<PrescribedMedicationDto>>> GetPrescribedMedicationsAsync(int sessionId)
        {
            var session = await _unitOfWork.ClinicalSessions.GetAsync(
                s => s.Id == sessionId,
                trackChanges: false);

            if (session == null)
            {
                return GeneralResult<List<PrescribedMedicationDto>>.NotFound(
                    $"Clinical Session with ID '{sessionId}' was not found.");
            }

            var prescribed = await _unitOfWork.PrescribedMedications.GetAllAsync(
                filter: m => m.SessionId == sessionId,
                sortBy: null,
                isDescending: false,
                trackChanges: false);

            var dtos = prescribed.Select(e => new PrescribedMedicationDto
            {
                Id = e.Id,
                DrugName = e.DrugName,
                Dosage = e.Dosage,
                Frequency = e.Frequency,
                Duration = e.Duration,
                Notes = e.Notes
            }).ToList();

            return GeneralResult<List<PrescribedMedicationDto>>.Success(dtos, "Prescriptions retrieved successfully.");
        }

        /// <inheritdoc />
        public async Task<GeneralResult<List<PrescribedMedicationDto>>> PrescribeMedicationsAsync(
            int sessionId,
            List<PrescribeMedicationRequest> medications)
        {
            // 1. Validate session exists
            var session = await _unitOfWork.ClinicalSessions.GetAsync(
                s => s.Id == sessionId,
                trackChanges: false);

            if (session == null)
            {
                return GeneralResult<List<PrescribedMedicationDto>>.NotFound(
                    $"Clinical Session with ID '{sessionId}' was not found.");
            }

            // 2. Verify the authenticated user is the doctor who owns this session
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return GeneralResult<List<PrescribedMedicationDto>>.Failure("User is not authenticated.");
            }

            var doctor = await _unitOfWork.Doctors.GetAsync(d => d.UserId == userId);
            if (doctor == null || doctor.Id != session.DoctorId)
            {
                return GeneralResult<List<PrescribedMedicationDto>>.Failure(
                    "You are not authorized to prescribe medications for this session.");
            }

            // 3. Validate the session is in a valid state
            if (session.Status != "In Progress" && session.Status != "Completed" && session.Status != "Processing")
            {
                return GeneralResult<List<PrescribedMedicationDto>>.Failure(
                    $"Cannot prescribe medications for a session with status '{session.Status}'.");
            }

            // 4. Map DTOs to entities and insert
            var entities = medications.Select(m => new PrescribedMedication
            {
                SessionId = sessionId,
                DrugName = m.DrugName.Trim(),
                Dosage = m.Dosage?.Trim(),
                Frequency = m.Frequency?.Trim(),
                Duration = m.Duration?.Trim(),
                Notes = m.Notes?.Trim()
            }).ToList();

            _unitOfWork.PrescribedMedications.AddRange(entities);
            await _unitOfWork.SaveChangesAsync();

            // 5. Map saved entities to response DTOs
            var resultDtos = entities.Select(e => new PrescribedMedicationDto
            {
                Id = e.Id,
                DrugName = e.DrugName,
                Dosage = e.Dosage,
                Frequency = e.Frequency,
                Duration = e.Duration,
                Notes = e.Notes
            }).ToList();

            return GeneralResult<List<PrescribedMedicationDto>>.Success(
                resultDtos,
                $"{resultDtos.Count} medication(s) prescribed successfully.");
        }

        /// <inheritdoc />
        public async Task<GeneralResult<List<PrescribedMedicationDto>>> UpdatePrescribedMedicationsAsync(
            int sessionId,
            List<PrescribeMedicationRequest> medications)
        {
            // 1. Validate session exists
            var session = await _unitOfWork.ClinicalSessions.GetAsync(
                s => s.Id == sessionId,
                trackChanges: false);

            if (session == null)
            {
                return GeneralResult<List<PrescribedMedicationDto>>.NotFound(
                    $"Clinical Session with ID '{sessionId}' was not found.");
            }

            // 2. Verify the authenticated user is the doctor who owns this session
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return GeneralResult<List<PrescribedMedicationDto>>.Failure("User is not authenticated.");
            }

            var doctor = await _unitOfWork.Doctors.GetAsync(d => d.UserId == userId);
            if (doctor == null || doctor.Id != session.DoctorId)
            {
                return GeneralResult<List<PrescribedMedicationDto>>.Failure(
                    "You are not Session's Doctor, so you are not authorized to update prescribed medications for this session.");
            }

            // 3. Validate the session is in a valid state
            if (session.Status != "In Progress" && session.Status != "Completed" && session.Status != "Processing")
            {
                return GeneralResult<List<PrescribedMedicationDto>>.Failure(
                    $"Cannot update prescribed medications for a session with status '{session.Status}'.");
            }

            // 4. Load existing prescribed medications for this session (tracked)
            var existingMeds = (await _unitOfWork.PrescribedMedications.GetAllAsync(
                filter: m => m.SessionId == sessionId,
                trackChanges: true)).ToList();

            var existingMedsDict = existingMeds.ToDictionary(m => m.Id);

            // 5. Validate provided IDs (must exist in this session & have no duplicates)
            var providedIds = medications
                .Where(m => m.Id.HasValue && m.Id.Value > 0)
                .Select(m => m.Id!.Value)
                .ToList();

            var duplicateIds = providedIds
                .GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Count > 0)
            {
                return GeneralResult<List<PrescribedMedicationDto>>.Failure(
                    $"Duplicate prescribed medication ID(s) found in request: {string.Join(", ", duplicateIds)}.");
            }

            var invalidIds = providedIds.Where(id => !existingMedsDict.ContainsKey(id)).ToList();
            if (invalidIds.Count > 0)
            {
                return GeneralResult<List<PrescribedMedicationDto>>.NotFound(
                    $"Prescribed medication ID(s) '{string.Join(", ", invalidIds)}' do not exist in this clinical session.");
            }

            // 6. Update tracked existing entities in-place and collect new ones (id is null or 0)
            var medsToAdd = new List<PrescribedMedication>();
            var updatedEntities = new List<PrescribedMedication>();

            foreach (var req in medications)
            {
                if (req.Id.HasValue && req.Id.Value > 0)
                {
                    // Existing medication -> update tracked entity
                    var existing = existingMedsDict[req.Id.Value];
                    existing.DrugName = req.DrugName.Trim();
                    existing.Dosage = req.Dosage?.Trim();
                    existing.Frequency = req.Frequency?.Trim();
                    existing.Duration = req.Duration?.Trim();
                    existing.Notes = req.Notes?.Trim();
                    updatedEntities.Add(existing);
                }
                else
                {
                    // New medication -> insert new entity
                    var newMed = new PrescribedMedication
                    {
                        SessionId = sessionId,
                        DrugName = req.DrugName.Trim(),
                        Dosage = req.Dosage?.Trim(),
                        Frequency = req.Frequency?.Trim(),
                        Duration = req.Duration?.Trim(),
                        Notes = req.Notes?.Trim()
                    };
                    medsToAdd.Add(newMed);
                    updatedEntities.Add(newMed);
                }
            }

            if (medsToAdd.Count > 0)
            {
                _unitOfWork.PrescribedMedications.AddRange(medsToAdd);
            }

            await _unitOfWork.SaveChangesAsync();

            // 7. Map to response DTOs
            var resultDtos = updatedEntities.Select(e => new PrescribedMedicationDto
            {
                Id = e.Id,
                DrugName = e.DrugName,
                Dosage = e.Dosage,
                Frequency = e.Frequency,
                Duration = e.Duration,
                Notes = e.Notes
            }).ToList();

            return GeneralResult<List<PrescribedMedicationDto>>.Success(
                resultDtos,
                $"{resultDtos.Count} medication(s) updated successfully.");
        }

        /// <inheritdoc />
        public async Task<GeneralResult> DeletePrescribedMedicationAsync(int sessionId, int medicationId)
        {
            // 1. Validate session exists
            var session = await _unitOfWork.ClinicalSessions.GetAsync(
                s => s.Id == sessionId,
                trackChanges: false);

            if (session == null)
            {
                return GeneralResult.NotFound(
                    $"Clinical Session with ID '{sessionId}' was not found.");
            }

            // 2. Verify the authenticated user is the doctor who owns this session
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return GeneralResult.Failure("User is not authenticated.");
            }

            var doctor = await _unitOfWork.Doctors.GetAsync(d => d.UserId == userId);
            if (doctor == null || doctor.Id != session.DoctorId)
            {
                return GeneralResult.Failure(
                    "You are not Session's Doctor, so you are not authorized to delete prescribed medications for this session.");
            }

            // 3. Validate the session is in a valid state
            if (session.Status != "In Progress" && session.Status != "Completed" && session.Status != "Processing")
            {
                return GeneralResult.Failure(
                    $"Cannot delete prescribed medications for a session with status '{session.Status}'.");
            }

            // 4. Find the prescribed medication
            var medication = await _unitOfWork.PrescribedMedications.GetAsync(
                filter: m => m.Id == medicationId && m.SessionId == sessionId,
                trackChanges: true);

            if (medication == null)
            {
                return GeneralResult.NotFound(
                    $"Prescribed medication with ID '{medicationId}' was not found in this clinical session.");
            }

            // 5. Delete and save
            _unitOfWork.PrescribedMedications.Delete(medication);
            await _unitOfWork.SaveChangesAsync();

            return GeneralResult.Success("Prescribed medication deleted successfully.");
        }

        /// <inheritdoc />
        public async Task<GeneralResult<string>> FinalizeTreatmentAsync(int sessionId)
        {
            // 1. Validate session exists
            var session = await _unitOfWork.ClinicalSessions.GetAsync(
                s => s.Id == sessionId,
                trackChanges: true,
                s => s.PrescribedMedications);

            if (session == null)
            {
                return GeneralResult<string>.NotFound(
                    $"Clinical Session with ID '{sessionId}' was not found.");
            }

            // 2. Verify the authenticated user is the doctor who owns this session
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return GeneralResult<string>.Failure("User is not authenticated.");
            }

            var doctor = await _unitOfWork.Doctors.GetAsync(d => d.UserId == userId);
            if (doctor == null || doctor.Id != session.DoctorId)
            {
                return GeneralResult<string>.Failure(
                    "You are not Session's Doctor, so you are not authorized to finalize treatment for this session.");
            }

            // 3. Load prescribed medications
            var prescribedMeds = session.PrescribedMedications?.ToList() ?? new List<PrescribedMedication>();

            if (prescribedMeds.Count > 0)
            {
                // 4. Load existing active medications for the patient (tracked)
                var existingActiveMeds = (await _unitOfWork.ActiveMedications.GetAllAsync(
                    filter: m => m.PatientId == session.PatientId && (m.EndDate == null || m.EndDate.Value.Date >= DateTime.UtcNow.Date),
                    trackChanges: true)).ToList();

                var existingActiveDict = existingActiveMeds
                    .GroupBy(m => m.DrugName.Trim().ToLower())
                    .ToDictionary(g => g.Key, g => g.First());

                var newActiveMeds = new List<ActiveMedication>();

                foreach (var pres in prescribedMeds)
                {
                    var drugKey = pres.DrugName.Trim().ToLower();
                    if (existingActiveDict.TryGetValue(drugKey, out var activeEntity))
                    {
                        // Update existing active medication
                        activeEntity.Dosage = pres.Dosage?.Trim();
                        activeEntity.Frequency = pres.Frequency?.Trim();
                        activeEntity.StartDate = DateTime.UtcNow;
                        activeEntity.SourceSessionId = session.Id;
                    }
                    else
                    {
                        // Insert new active medication
                        var newActive = new ActiveMedication
                        {
                            PatientId = session.PatientId,
                            DrugName = pres.DrugName.Trim(),
                            Dosage = pres.Dosage?.Trim(),
                            Frequency = pres.Frequency?.Trim(),
                            StartDate = DateTime.UtcNow,
                            SourceSessionId = session.Id
                        };
                        newActiveMeds.Add(newActive);
                        existingActiveDict[drugKey] = newActive;
                    }
                }

                if (newActiveMeds.Count > 0)
                {
                    _unitOfWork.ActiveMedications.AddRange(newActiveMeds);
                }
            }

            // 5. Update session status
            session.Status = "Completed";
            session.FinalizedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            return GeneralResult<string>.Success(
                "Treatment finalized successfully. Prescriptions added to active medications.",
                "Success");
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
