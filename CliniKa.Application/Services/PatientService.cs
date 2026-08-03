using CliniKa.Application.Abstraction;
using CliniKa.Application.Abstractions.Authentication;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Patient;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using FluentValidation;

namespace CliniKa.Application.Services
{
    public class PatientService : IPatientService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<AddMedicalInfoRequest> _addMedicalInfoValidator;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserService _userService;

        public PatientService(
            IUnitOfWork unitOfWork,
            IValidator<AddMedicalInfoRequest> addMedicalInfoValidator,
            ICurrentUserService currentUserService,
            IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _addMedicalInfoValidator = addMedicalInfoValidator;
            _currentUserService = currentUserService;
            _userService = userService;
        }

        public async Task<GeneralResult<PatientReadDto>> GetPatientAsync(string query)
        {
            var patient = await _unitOfWork.Patient.GetAsync(
                    filter: p => p.NationalId!.Contains(query) || p.FullName.Contains(query),
                    trackChanges: false
                );

            if (patient == null)
            {
                return GeneralResult<PatientReadDto>.NotFound("Patient not found.");
            }

            var patientReadDTO = new PatientReadDto
            {
                Id = patient.Id,
                FullName = patient.FullName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                BloodType = patient.BloodType,
                NationalId = patient.NationalId
            };

            return GeneralResult<PatientReadDto>.Success(patientReadDTO, "Patient retrieved successfully.");
        }

        public async Task<GeneralResult<PatientHistoryReadDto>> GetPatientHistoryAsync(int patientId)
        {
            var patient = await _unitOfWork.Patient.GetPatientWithIncludesAsync(patientId);

            if (patient == null)
            {
                return GeneralResult<PatientHistoryReadDto>.NotFound("Patient not found.");
            }

            var patientHistoryDto = new PatientHistoryReadDto
            {
                PatientId = patient.Id,
                FullName = patient.FullName,
                DateOfBirth = patient.DateOfBirth,

                Diseases = patient.PatientDiseases.Select(pd => new DiseaseDto
                {
                    DiseaseCode = pd.Disease != null ? pd.Disease.DiseaseCode : "CUSTOM",
                    DiseaseName = pd.Disease != null ? pd.Disease.DiseaseName : (pd.RawDiseaseName ?? string.Empty),
                    DiagnosedDate = pd.DiagnosedDate.ToString("yyyy-MM-dd")
                }).ToList(),

                Allergies = patient.PatientAllergies.Select(pa => new AllergyDto
                {
                    AllergenName = pa.Allergy != null ? pa.Allergy.AllergenName : (pa.RawAllergenName ?? string.Empty),
                    Severity = pa.Severity ?? string.Empty
                }).ToList(),

                ActiveMedications = patient.ActiveMedications.Where(am => am.IsActive).Select(am => new MedicationDto
                {
                    DrugName = am.DrugName,
                    Dosage = am.Dosage ?? string.Empty,
                    Frequency = am.Frequency ?? string.Empty,
                    StartDate = am.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = am.EndDate?.ToString("yyyy-MM-dd") ?? ""
                }).ToList()
            };

            return GeneralResult<PatientHistoryReadDto>.Success(patientHistoryDto, "Patient history retrieved successfully.");
        }

        public async Task<GeneralResult> AddMedicalInfoAsync(AddMedicalInfoRequest request)
        {
            // 1. Validate request
            var validationResult = await _addMedicalInfoValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult.ValidationFailure(errors);
            }

            // 2. Get authenticated user ID from Token
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return GeneralResult.Failure("User is not authenticated.");
            }

            // 3. Find patient linked to the authenticated user
            var patient = await _unitOfWork.Patient.GetAsync(p => p.UserId == userId , trackChanges:true);
            if (patient == null)
            {
                return GeneralResult.NotFound("Patient profile not found for the authenticated user.");
            }

            int patientId = patient.Id;

            // 4. Validate existence of referenced DiseaseIds (if provided)
            if (request.Diseases != null && request.Diseases.Any())
            {
                foreach (var d in request.Diseases)
                {
                    if (d.DiseaseId.HasValue)
                    {
                        var diseaseExists = await _unitOfWork.Diseases.GetByIdAsync(d.DiseaseId.Value);
                        if (diseaseExists == null)
                        {
                            return GeneralResult.NotFound($"Disease with ID '{d.DiseaseId.Value}' was not found.");
                        }
                    }
                }
            }

            // 5. Validate existence of referenced AllergyIds (if provided)
            if (request.Allergies != null && request.Allergies.Any())
            {
                foreach (var a in request.Allergies)
                {
                    if (a.AllergyId.HasValue)
                    {
                        var allergyExists = await _unitOfWork.Allergies.GetByIdAsync(a.AllergyId.Value);
                        if (allergyExists == null)
                        {
                            return GeneralResult.NotFound($"Allergy with ID '{a.AllergyId.Value}' was not found.");
                        }
                    }
                }
            }

            // 6. Update basic medical fields & national ID (overwrite if provided)
            if (!string.IsNullOrWhiteSpace(request.Gender))
                patient.Gender = request.Gender;

            if (!string.IsNullOrWhiteSpace(request.BloodType))
                patient.BloodType = request.BloodType;

            if (!string.IsNullOrWhiteSpace(request.NationalId))
                patient.NationalId = request.NationalId;

            if (request.DateOfBirth.HasValue)
                patient.DateOfBirth = request.DateOfBirth;

            patient.CompletedAt = DateTime.UtcNow;

            // 7. Add diseases
            if (request.Diseases != null && request.Diseases.Any())
            {
                var diseaseEntities = new List<PatientDisease>();
                foreach (var d in request.Diseases)
                {
                    diseaseEntities.Add(new PatientDisease
                    {
                        PatientId = patientId,
                        DiseaseId = d.DiseaseId,
                        RawDiseaseName = d.DiseaseId.HasValue ? null : d.RawDiseaseName,
                        DiagnosedDate = d.DiagnosedDate ?? DateTime.UtcNow
                    });
                }

                foreach (var entity in diseaseEntities)
                {
                    patient.PatientDiseases.Add(entity);
                }
            }

            // 8. Add allergies
            if (request.Allergies != null && request.Allergies.Any())
            {
                var allergyEntities = new List<PatientAllergy>();
                foreach (var a in request.Allergies)
                {
                    allergyEntities.Add(new PatientAllergy
                    {
                        PatientId = patientId,
                        AllergyId = a.AllergyId,
                        RawAllergenName = a.AllergyId.HasValue ? null : a.RawAllergenName,
                        Severity = a.Severity
                    });
                }

                foreach (var entity in allergyEntities)
                {
                    patient.PatientAllergies.Add(entity);
                }
            }

            // 7. Add medications
            if (request.Medications != null && request.Medications.Any())
            {
                var medicationEntities = request.Medications.Select(m => new ActiveMedication
                {
                    PatientId = patientId,
                    DrugName = m.DrugName,
                    Dosage = m.Dosage,
                    Frequency = m.Frequency,
                    StartDate = DateTime.UtcNow
                });

                _unitOfWork.ActiveMedications.AddRange(medicationEntities);
            }

            // 8. Persist all changes
            await _unitOfWork.SaveChangesAsync();

            return GeneralResult.Success("Medical information added successfully.");
        }

        public async Task<GeneralResult<PatientProfileDto>> GetPatientProfileAsync()
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return GeneralResult<PatientProfileDto>.Failure("User is not authenticated.");
            }

            var userResult = await _userService.GetUserByIdAsync(userId);
            if (!userResult.IsSuccess || userResult.Data == null)
            {
                return GeneralResult<PatientProfileDto>.NotFound("User identity profile not found.");
            }

            var userDto = userResult.Data;
            var patient = await _unitOfWork.Patient.GetAsync(p => p.UserId == userId, trackChanges: false);

            var profileDto = new PatientProfileDto
            {
                PatientId = patient?.Id ?? 0,
                UserId = userDto.Id,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Phone = userDto.PhoneNumber,
                DateOfBirth = patient?.DateOfBirth,
                Gender = patient?.Gender,
                BloodType = patient?.BloodType,
                NationalId = patient?.NationalId,
                CompletedAt = patient?.CompletedAt
            };

            return GeneralResult<PatientProfileDto>.Success(profileDto, "Patient profile retrieved successfully.");
        }

        public async Task<GeneralResult<PatientMedicalDataDto>> GetPatientMedicalDataAsync()
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return GeneralResult<PatientMedicalDataDto>.Failure("User is not authenticated.");
            }

            var patient = await _unitOfWork.Patient.GetPatientFullMedicalDataByUserIdAsync(userId);
            if (patient == null)
            {
                return GeneralResult<PatientMedicalDataDto>.NotFound("Patient profile not found.");
            }

            var medicalDataDto = new PatientMedicalDataDto
            {
                PatientId = patient.Id,
                FullName = patient.FullName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                BloodType = patient.BloodType,

                ActiveMedications = patient.ActiveMedications.Where(am => am.IsActive).Select(am => new MedicationDto
                {
                    DrugName = am.DrugName,
                    Dosage = am.Dosage ?? string.Empty,
                    Frequency = am.Frequency ?? string.Empty,
                    StartDate = am.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = am.EndDate?.ToString("yyyy-MM-dd") ?? ""
                }).ToList(),

                Diseases = patient.PatientDiseases.Select(pd => new DiseaseDto
                {
                    DiseaseCode = pd.Disease != null ? pd.Disease.DiseaseCode : "CUSTOM",
                    DiseaseName = pd.Disease != null ? pd.Disease.DiseaseName : (pd.RawDiseaseName ?? string.Empty),
                    DiagnosedDate = pd.DiagnosedDate.ToString("yyyy-MM-dd")
                }).ToList(),

                Allergies = patient.PatientAllergies.Select(pa => new AllergyDto
                {
                    AllergenName = pa.Allergy != null ? pa.Allergy.AllergenName : (pa.RawAllergenName ?? string.Empty),
                    Severity = pa.Severity ?? string.Empty
                }).ToList(),

                Sessions = patient.ClinicalSessions.Select(cs => new PatientSessionDto
                {
                    SessionId = cs.Id,
                    VisitDate = cs.VisitDate,
                    DoctorName = cs.Doctor != null ? cs.Doctor.FullName : string.Empty,
                    DoctorSpecialty = cs.Doctor?.Specialty,
                    Status = cs.Status,
                    FinalDiagnosis = cs.FinalDiagnosis
                }).OrderByDescending(s => s.VisitDate).ToList()
            };

            return GeneralResult<PatientMedicalDataDto>.Success(medicalDataDto, "Patient medical data retrieved successfully.");
        }

        public async Task<GeneralResult<IEnumerable<PatientSessionDto>>> GetPatientSessionsAsync()
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return GeneralResult<IEnumerable<PatientSessionDto>>.Failure("User is not authenticated.");
            }

            var sessions = await _unitOfWork.ClinicalSessions.GetSessionsByPatientUserIdAsync(userId);
            var sessionDtos = sessions.Select(cs => new PatientSessionDto
            {
                SessionId = cs.Id,
                VisitDate = cs.VisitDate,
                DoctorName = cs.Doctor != null ? cs.Doctor.FullName : string.Empty,
                DoctorSpecialty = cs.Doctor?.Specialty,
                Status = cs.Status,
                FinalDiagnosis = cs.FinalDiagnosis
            }).ToList();

            return GeneralResult<IEnumerable<PatientSessionDto>>.Success(sessionDtos, "Patient sessions retrieved successfully.");
        }

        public async Task<GeneralResult<PatientSessionDetailsDto>> GetPatientSessionDetailsAsync(int sessionId)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return GeneralResult<PatientSessionDetailsDto>.Failure("User is not authenticated.");
            }

            var session = await _unitOfWork.ClinicalSessions.GetSessionDetailsByPatientUserIdAsync(sessionId, userId);
            if (session == null)
            {
                return GeneralResult<PatientSessionDetailsDto>.NotFound($"Session with ID '{sessionId}' was not found or access is denied.");
            }

            var detailsDto = new PatientSessionDetailsDto
            {
                SessionId = session.Id,
                PatientId = session.PatientId,
                PatientName = session.Patient != null ? session.Patient.FullName : string.Empty,
                DoctorId = session.DoctorId,
                DoctorName = session.Doctor != null ? session.Doctor.FullName : string.Empty,
                DoctorSpecialty = session.Doctor?.Specialty,
                VisitDate = session.VisitDate,
                Status = session.Status,
                FinalizedAt = session.FinalizedAt,
                AudioTranscript = session.AudioTranscript,
                ExtractedSymptoms = session.ExtractedSymptoms,
                MentionedMedications = session.MentionedMedications,
                MentionedHistory = session.MentionedHistory,
                FinalDiagnosis = session.FinalDiagnosis,
                DoctorNotes = session.DoctorNotes,
                FollowUpRecommendations = session.FollowUpRecommendations,
                AiReport = session.SessionAiReport != null ? new SessionAiReportDto
                {
                    Id = session.SessionAiReport.Id,
                    PatientSummary = session.SessionAiReport.PatientSummary,
                    PossibleDiagnoses = session.SessionAiReport.PossibleDiagnoses,
                    DrugInteractions = session.SessionAiReport.DrugInteractions,
                    Contraindications = session.SessionAiReport.Contraindications,
                    SuggestedInvestigations = session.SessionAiReport.SuggestedInvestigations,
                    ClinicalAlerts = session.SessionAiReport.ClinicalAlerts,
                    GeneratedAt = session.SessionAiReport.GeneratedAt
                } : null,
                PrescribedMedications = session.PrescribedMedications.Select(pm => new PrescribedMedicationDto
                {
                    Id = pm.Id,
                    DrugName = pm.DrugName,
                    Dosage = pm.Dosage,
                    Frequency = pm.Frequency,
                    Duration = pm.Duration,
                    Notes = pm.Notes
                }).ToList(),
                UploadedDocuments = session.UploadedDocuments.Select(ud => new UploadedDocumentDto
                {
                    Id = ud.Id,
                    DocumentType = ud.DocumentType,
                    FilePath = ud.FilePath,
                    ExtractedText = ud.ExtractedText,
                    UploadedAt = ud.UploadedAt
                }).ToList()
            };

            return GeneralResult<PatientSessionDetailsDto>.Success(detailsDto, "Session details retrieved successfully.");
        }
    }
}
