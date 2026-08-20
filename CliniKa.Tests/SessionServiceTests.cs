using CliniKa.Application;
using CliniKa.Application.Abstractions.Authentication;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Langflow;
using CliniKa.Application.DTOs.Sessions;
using CliniKa.Application.Services;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Linq.Expressions;

namespace CliniKa.Tests
{
    public class SessionServiceTests
    {
        private readonly Mock<IClinicalSessionRepository> _mockSessionRepo;
        private readonly Mock<IDoctorRepository> _mockDoctorRepo;
        private readonly Mock<IPrescribedMedicationsRepository> _mockPrescribedRepo;
        private readonly Mock<IActiveMedicationsRepository> _mockActiveMedicationsRepo;
        private readonly Mock<ISessionDifferentialDiagnosisRepository> _mockDiffRepo;
        private readonly Mock<ISessionRecommendedInvestigationRepository> _mockRecInvestRepo;
        private readonly Mock<ILangFlowService> _mockLangFlowService;
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly SessionService _sessionService;

        public SessionServiceTests()
        {
            _mockSessionRepo = new Mock<IClinicalSessionRepository>();
            _mockDoctorRepo = new Mock<IDoctorRepository>();
            _mockPrescribedRepo = new Mock<IPrescribedMedicationsRepository>();
            _mockActiveMedicationsRepo = new Mock<IActiveMedicationsRepository>();
            _mockDiffRepo = new Mock<ISessionDifferentialDiagnosisRepository>();
            _mockRecInvestRepo = new Mock<ISessionRecommendedInvestigationRepository>();
            _mockLangFlowService = new Mock<ILangFlowService>();
            _mockFileService = new Mock<IFileService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _mockUnitOfWork.Setup(u => u.ClinicalSessions).Returns(_mockSessionRepo.Object);
            _mockUnitOfWork.Setup(u => u.Doctors).Returns(_mockDoctorRepo.Object);
            _mockUnitOfWork.Setup(u => u.PrescribedMedications).Returns(_mockPrescribedRepo.Object);
            _mockUnitOfWork.Setup(u => u.ActiveMedications).Returns(_mockActiveMedicationsRepo.Object);
            _mockUnitOfWork.Setup(u => u.SessionDifferentialDiagnoses).Returns(_mockDiffRepo.Object);
            _mockUnitOfWork.Setup(u => u.SessionRecommendedInvestigations).Returns(_mockRecInvestRepo.Object);

            _sessionService = new SessionService(
                _mockLangFlowService.Object,
                _mockFileService.Object,
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object
            );
        }

        [Fact]
        public async Task CreateSessionAsync_ShouldAddSessionAndSave_WhenValidDtoIsProvided()
        {
            var dto = new CreateClinicalSessionDto(1);

            _mockCurrentUserService.Setup(c => c.UserId).Returns("doctor-user-id");

            var doctor = new Doctor { Id = 2, UserId = "doctor-user-id" };
            _mockDoctorRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Doctor, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Doctor, object>>[]>()
            )).ReturnsAsync(doctor);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sessionService.CreateSessionAsync(dto);

            Assert.True(result.IsSuccess);
            _mockSessionRepo.Verify(
                r => r.Add(It.Is<ClinicalSession>(s => s.PatientId == 1 && s.DoctorId == 2)),
                Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DiagnoseSessionAsync_ShouldReturnNotFound_WhenSessionDoesNotExist()
        {
            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync((ClinicalSession?)null);

            var mockAudio = new Mock<IFormFile>();
            mockAudio.Setup(a => a.OpenReadStream()).Returns(new MemoryStream());
            mockAudio.Setup(a => a.FileName).Returns("audio.wav");

            var result = await _sessionService.DiagnoseSessionAsync(999, mockAudio.Object, null, "http", "localhost", "C:\\mock");

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task DiagnoseSessionAsync_ShouldUploadFilesAndSetProcessingStatus_WhenSuccessful()
        {
            var sessionId = 123;
            var session = new ClinicalSession
            {
                Id = sessionId,
                DoctorId = 1,
                PatientId = 2,
                Status = "In Progress"
            };

            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync(session);

            _mockFileService.Setup(f => f.UploadFileAsync(
                It.IsAny<Application.DTOs.FormFile.UploadFileRequest>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()
            )).ReturnsAsync(GeneralResult<string>.Success("http://localhost/uploads/audio.wav", "File uploaded."));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var mockAudio = new Mock<IFormFile>();
            mockAudio.Setup(a => a.OpenReadStream()).Returns(new MemoryStream());
            mockAudio.Setup(a => a.FileName).Returns("audio.wav");

            var mockDoc = new Mock<IFormFile>();
            mockDoc.Setup(d => d.OpenReadStream()).Returns(new MemoryStream());
            mockDoc.Setup(d => d.FileName).Returns("scan.png");
            mockDoc.Setup(d => d.Length).Returns(1024);

            var docsList = new List<IFormFile> { mockDoc.Object };

            var result = await _sessionService.DiagnoseSessionAsync(sessionId, mockAudio.Object, docsList, "http", "localhost", "C:\\mock");

            Assert.True(result.IsSuccess);
            Assert.Equal("Processing started", result.Data);
            Assert.Equal("Processing", session.Status);

            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SaveDiagnosisCallbackResultAsync_ShouldUpdateSessionAndAiReportAndSaveDifferentialDiagnoses_WhenSuccessful()
        {
            var sessionId = 123;
            var session = new ClinicalSession
            {
                Id = sessionId,
                DoctorId = 1,
                PatientId = 2,
                Status = "Processing"
            };

            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync(session);

            _mockDiffRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<SessionDifferentialDiagnosis, bool>>>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<SessionDifferentialDiagnosis, object>>[]>()
            )).ReturnsAsync(new List<SessionDifferentialDiagnosis>());

            _mockRecInvestRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<SessionRecommendedInvestigation, bool>>>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<SessionRecommendedInvestigation, object>>[]>()
            )).ReturnsAsync(new List<SessionRecommendedInvestigation>());

            var callbackResult = new LangFlowDiagnosisResult
            {
                Status = 200,
                Body = new LangFlowDiagnosisResultBody
                {
                    Transcript = "Doctor: Hello. Patient: Hello doctor.",
                    Analysis = "Clinical evaluation notes",
                    ChiefComplaint = "Fever and cough",
                    HistoryOfPresentIllness = "Feeling unwell for 4 days",
                    CompleteFindingsFromAllMedicalImages = "Normal chest radiograph",
                    PossibleDiagnosis = "Viral upper respiratory infection",
                    Top5DifferentialDiagnoses = new List<string>
                    {
                        "Acute Bronchitis",
                        "Influenza",
                        "COVID-19",
                        "Pneumonia",
                        "Common Cold"
                    },
                    RecommendedInvestigations = new List<string> { "CBC", "Influenza swab" },
                    ImmediateManagement = "Rest and hydration",
                    ImagesReceived = 1
                }
            };

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sessionService.SaveDiagnosisCallbackResultAsync(sessionId, callbackResult);

            Assert.True(result.IsSuccess);
            Assert.Equal("Completed", session.Status);

            Assert.NotNull(session.SessionAiReport);
            Assert.Equal("Doctor: Hello. Patient: Hello doctor.", session.SessionAiReport.Transcript);
            Assert.Equal("Clinical evaluation notes", session.SessionAiReport.Analysis);
            Assert.Equal("Fever and cough", session.SessionAiReport.ChiefComplaint);
            Assert.Equal("Feeling unwell for 4 days", session.SessionAiReport.HistoryOfPresentIllness);
            Assert.Equal("Normal chest radiograph", session.SessionAiReport.CompleteFindingsFromAllMedicalImages);
            Assert.Equal("Viral upper respiratory infection", session.SessionAiReport.PossibleDiagnosis);
            Assert.Equal("Rest and hydration", session.SessionAiReport.ImmediateManagement);
            Assert.Equal(1, session.SessionAiReport.ImagesReceived);

            _mockDiffRepo.Verify(r => r.AddRange(It.Is<List<SessionDifferentialDiagnosis>>(list =>
                list.Count == 5 &&
                list[0].Diagnosis == "Acute Bronchitis" && list[0].Rank == 1 &&
                list[4].Diagnosis == "Common Cold" && list[4].Rank == 5
            )), Times.Once);

            _mockRecInvestRepo.Verify(r => r.AddRange(It.Is<List<SessionRecommendedInvestigation>>(list =>
                list.Count == 2 &&
                list[0].Investigation == "CBC" && list[0].Order == 1 &&
                list[1].Investigation == "Influenza swab" && list[1].Order == 2
            )), Times.Once);

            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetSessionDiagnosisAsync_ShouldReturnNotFound_WhenSessionDoesNotExist()
        {
            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync((ClinicalSession?)null);

            var result = await _sessionService.GetSessionDiagnosisAsync(999);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task GetSessionDiagnosisAsync_ShouldReturnCallbackFormatResponse_WhenSessionExists()
        {
            var sessionId = 123;
            var session = new ClinicalSession
            {
                Id = sessionId,
                Status = "Completed",
                SessionAiReport = new SessionAiReport
                {
                    SessionId = sessionId,
                    Transcript = "Doctor transcript",
                    Analysis = "Detailed analysis",
                    ChiefComplaint = "Cough",
                    HistoryOfPresentIllness = "Started 2 days ago",
                    CompleteFindingsFromAllMedicalImages = "Clear lungs",
                    PossibleDiagnosis = "Bronchitis",
                    ImmediateManagement = "Inhaler as needed",
                    ImagesReceived = 2
                },
                DifferentialDiagnoses = new List<SessionDifferentialDiagnosis>
                {
                    new SessionDifferentialDiagnosis { SessionId = sessionId, Diagnosis = "Bronchitis", Rank = 1 },
                    new SessionDifferentialDiagnosis { SessionId = sessionId, Diagnosis = "Asthma", Rank = 2 },
                    new SessionDifferentialDiagnosis { SessionId = sessionId, Diagnosis = "GERD", Rank = 3 },
                    new SessionDifferentialDiagnosis { SessionId = sessionId, Diagnosis = "Pneumonia", Rank = 4 },
                    new SessionDifferentialDiagnosis { SessionId = sessionId, Diagnosis = "Post-nasal drip", Rank = 5 }
                },
                RecommendedInvestigations = new List<SessionRecommendedInvestigation>
                {
                    new SessionRecommendedInvestigation { SessionId = sessionId, Investigation = "Chest X-ray", Order = 1 },
                    new SessionRecommendedInvestigation { SessionId = sessionId, Investigation = "CBC", Order = 2 }
                }
            };

            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync(session);

            _mockDiffRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<SessionDifferentialDiagnosis, bool>>>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<SessionDifferentialDiagnosis, object>>[]>()
            )).ReturnsAsync(session.DifferentialDiagnoses.ToList());

            _mockRecInvestRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<SessionRecommendedInvestigation, bool>>>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<SessionRecommendedInvestigation, object>>[]>()
            )).ReturnsAsync(session.RecommendedInvestigations.ToList());

            var result = await _sessionService.GetSessionDiagnosisAsync(sessionId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(200, result.Data.Status);
            Assert.NotNull(result.Data.Body);
            Assert.Equal("Doctor transcript", result.Data.Body.Transcript);
            Assert.Equal("Detailed analysis", result.Data.Body.Analysis);
            Assert.Equal("Cough", result.Data.Body.ChiefComplaint);
            Assert.Equal("Started 2 days ago", result.Data.Body.HistoryOfPresentIllness);
            Assert.Equal("Clear lungs", result.Data.Body.CompleteFindingsFromAllMedicalImages);
            Assert.Equal("Bronchitis", result.Data.Body.PossibleDiagnosis);
            Assert.Equal("Inhaler as needed", result.Data.Body.ImmediateManagement);
            Assert.Equal(2, result.Data.Body.ImagesReceived);

            Assert.Equal(5, result.Data.Body.Top5DifferentialDiagnoses?.Count);
            Assert.Equal("Bronchitis", result.Data.Body.Top5DifferentialDiagnoses?[0]);
            Assert.Equal("Post-nasal drip", result.Data.Body.Top5DifferentialDiagnoses?[4]);

            Assert.Equal(2, result.Data.Body.RecommendedInvestigations?.Count);
            Assert.Equal("Chest X-ray", result.Data.Body.RecommendedInvestigations?[0]);
            Assert.Equal("CBC", result.Data.Body.RecommendedInvestigations?[1]);
        }

        [Fact]
        public async Task UpdatePrescribedMedicationsAsync_ShouldReturnNotFound_WhenSessionDoesNotExist()
        {
            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync((ClinicalSession?)null);

            var meds = new List<PrescribeMedicationRequest>
            {
                new PrescribeMedicationRequest { DrugName = "Amoxicillin", Dosage = "500mg" }
            };

            var result = await _sessionService.UpdatePrescribedMedicationsAsync(999, meds);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task UpdatePrescribedMedicationsAsync_ShouldReturnFailure_WhenDoctorNotAuthorized()
        {
            var session = new ClinicalSession { Id = 1, DoctorId = 5, Status = "In Progress" };
            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync(session);

            _mockCurrentUserService.Setup(c => c.UserId).Returns("other-doctor-user-id");

            var doctor = new Doctor { Id = 99, UserId = "other-doctor-user-id" };
            _mockDoctorRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Doctor, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Doctor, object>>[]>()
            )).ReturnsAsync(doctor);

            var meds = new List<PrescribeMedicationRequest>
            {
                new PrescribeMedicationRequest { DrugName = "Amoxicillin", Dosage = "500mg" }
            };

            var result = await _sessionService.UpdatePrescribedMedicationsAsync(1, meds);

            Assert.False(result.IsSuccess);
            Assert.Contains("not authorized", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdatePrescribedMedicationsAsync_ShouldUpdateTrackedEntitiesAndAddNew_WithoutDeletingOthers()
        {
            var sessionId = 10;
            var session = new ClinicalSession { Id = sessionId, DoctorId = 2, Status = "In Progress" };

            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync(session);

            _mockCurrentUserService.Setup(c => c.UserId).Returns("doc-user-id");

            var doctor = new Doctor { Id = 2, UserId = "doc-user-id" };
            _mockDoctorRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Doctor, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Doctor, object>>[]>()
            )).ReturnsAsync(doctor);

            var existingMed1 = new PrescribedMedication { Id = 1, SessionId = sessionId, DrugName = "Amoxicillin", Dosage = "250mg" };
            var existingMed2 = new PrescribedMedication { Id = 2, SessionId = sessionId, DrugName = "UntouchedMed", Dosage = "10mg" };
            var existingMeds = new List<PrescribedMedication> { existingMed1, existingMed2 };

            _mockPrescribedRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<PrescribedMedication, bool>>>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<PrescribedMedication, object>>[]>()
            )).ReturnsAsync(existingMeds);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var incomingMeds = new List<PrescribeMedicationRequest>
            {
                // Update existing entity 1
                new PrescribeMedicationRequest { Id = 1, DrugName = "Amoxicillin Updated", Dosage = "500mg", Frequency = "Twice daily" },
                // Add brand new entity (Id is null)
                new PrescribeMedicationRequest { Id = null, DrugName = "Panadol", Dosage = "1000mg", Frequency = "Once daily" }
                // Notice: entity 2 (UntouchedMed) is omitted, and should NOT be deleted
            };

            var result = await _sessionService.UpdatePrescribedMedicationsAsync(sessionId, incomingMeds);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Data!.Count);

            // Entity 1 was updated in-place
            Assert.Equal("Amoxicillin Updated", existingMed1.DrugName);
            Assert.Equal("500mg", existingMed1.Dosage);
            Assert.Equal("Twice daily", existingMed1.Frequency);

            // DeleteRange should NOT have been called
            _mockPrescribedRepo.Verify(r => r.DeleteRange(It.IsAny<IEnumerable<PrescribedMedication>>()), Times.Never);

            // New entity was added
            _mockPrescribedRepo.Verify(r => r.AddRange(It.Is<List<PrescribedMedication>>(l => l.Count == 1 && l[0].DrugName == "Panadol")), Times.Once);

            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdatePrescribedMedicationsAsync_ShouldReturnNotFound_WhenProvidedIdDoesNotExistInSession()
        {
            var sessionId = 10;
            var session = new ClinicalSession { Id = sessionId, DoctorId = 2, Status = "In Progress" };

            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync(session);

            _mockCurrentUserService.Setup(c => c.UserId).Returns("doc-user-id");

            var doctor = new Doctor { Id = 2, UserId = "doc-user-id" };
            _mockDoctorRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Doctor, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Doctor, object>>[]>()
            )).ReturnsAsync(doctor);

            var existingMeds = new List<PrescribedMedication>
            {
                new PrescribedMedication { Id = 1, SessionId = sessionId, DrugName = "Amoxicillin" }
            };

            _mockPrescribedRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<PrescribedMedication, bool>>>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<PrescribedMedication, object>>[]>()
            )).ReturnsAsync(existingMeds);

            var incomingMeds = new List<PrescribeMedicationRequest>
            {
                new PrescribeMedicationRequest { Id = 999, DrugName = "InvalidIdDrug" }
            };

            var result = await _sessionService.UpdatePrescribedMedicationsAsync(sessionId, incomingMeds);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Contains("999", result.Message);
        }

        [Fact]
        public async Task UpdatePrescribedMedicationsAsync_ShouldReturnFailure_WhenDuplicateIdsProvided()
        {
            var sessionId = 10;
            var session = new ClinicalSession { Id = sessionId, DoctorId = 2, Status = "In Progress" };

            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync(session);

            _mockCurrentUserService.Setup(c => c.UserId).Returns("doc-user-id");

            var doctor = new Doctor { Id = 2, UserId = "doc-user-id" };
            _mockDoctorRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Doctor, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Doctor, object>>[]>()
            )).ReturnsAsync(doctor);

            var existingMeds = new List<PrescribedMedication>
            {
                new PrescribedMedication { Id = 1, SessionId = sessionId, DrugName = "Amoxicillin" }
            };

            _mockPrescribedRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<PrescribedMedication, bool>>>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<PrescribedMedication, object>>[]>()
            )).ReturnsAsync(existingMeds);

            var incomingMeds = new List<PrescribeMedicationRequest>
            {
                new PrescribeMedicationRequest { Id = 1, DrugName = "Amoxicillin 1" },
                new PrescribeMedicationRequest { Id = 1, DrugName = "Amoxicillin 2" }
            };

            var result = await _sessionService.UpdatePrescribedMedicationsAsync(sessionId, incomingMeds);

            Assert.False(result.IsSuccess);
            Assert.Contains("Duplicate", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeletePrescribedMedicationAsync_ShouldReturnNotFound_WhenMedicationDoesNotExistInSession()
        {
            var sessionId = 10;
            var session = new ClinicalSession { Id = sessionId, DoctorId = 2, Status = "In Progress" };

            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync(session);

            _mockCurrentUserService.Setup(c => c.UserId).Returns("doc-user-id");

            var doctor = new Doctor { Id = 2, UserId = "doc-user-id" };
            _mockDoctorRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Doctor, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Doctor, object>>[]>()
            )).ReturnsAsync(doctor);

            _mockPrescribedRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<PrescribedMedication, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<PrescribedMedication, object>>[]>()
            )).ReturnsAsync((PrescribedMedication?)null);

            var result = await _sessionService.DeletePrescribedMedicationAsync(sessionId, 999);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task DeletePrescribedMedicationAsync_ShouldDeleteAndSave_WhenSuccessful()
        {
            var sessionId = 10;
            var session = new ClinicalSession { Id = sessionId, DoctorId = 2, Status = "In Progress" };

            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync(session);

            _mockCurrentUserService.Setup(c => c.UserId).Returns("doc-user-id");

            var doctor = new Doctor { Id = 2, UserId = "doc-user-id" };
            _mockDoctorRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Doctor, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Doctor, object>>[]>()
            )).ReturnsAsync(doctor);

            var medication = new PrescribedMedication { Id = 5, SessionId = sessionId, DrugName = "Aspirin" };
            _mockPrescribedRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<PrescribedMedication, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<PrescribedMedication, object>>[]>()
            )).ReturnsAsync(medication);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sessionService.DeletePrescribedMedicationAsync(sessionId, 5);

            Assert.True(result.IsSuccess);
            _mockPrescribedRepo.Verify(r => r.Delete(medication), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task FinalizeTreatmentAsync_ShouldReturnNotFound_WhenSessionDoesNotExist()
        {
            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync((ClinicalSession?)null);

            var result = await _sessionService.FinalizeTreatmentAsync(999);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task FinalizeTreatmentAsync_ShouldReturnFailure_WhenDoctorNotAuthorized()
        {
            var session = new ClinicalSession { Id = 1, DoctorId = 5, Status = "In Progress" };
            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync(session);

            _mockCurrentUserService.Setup(c => c.UserId).Returns("other-doctor-user-id");

            var doctor = new Doctor { Id = 99, UserId = "other-doctor-user-id" };
            _mockDoctorRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Doctor, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Doctor, object>>[]>()
            )).ReturnsAsync(doctor);

            var result = await _sessionService.FinalizeTreatmentAsync(1);

            Assert.False(result.IsSuccess);
            Assert.Contains("not authorized", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task FinalizeTreatmentAsync_ShouldPersistPrescribedMedsToActiveMedicationsAndCompleteSession()
        {
            var sessionId = 10;
            var patientId = 3;
            var session = new ClinicalSession
            {
                Id = sessionId,
                PatientId = patientId,
                DoctorId = 2,
                Status = "In Progress",
                PrescribedMedications = new List<PrescribedMedication>
                {
                    new PrescribedMedication { Id = 1, SessionId = sessionId, DrugName = "Metformin", Dosage = "500mg", Frequency = "Twice daily" },
                    new PrescribedMedication { Id = 2, SessionId = sessionId, DrugName = "Aspirin", Dosage = "81mg", Frequency = "Once daily" }
                }
            };

            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync(session);

            _mockCurrentUserService.Setup(c => c.UserId).Returns("doc-user-id");

            var doctor = new Doctor { Id = 2, UserId = "doc-user-id" };
            _mockDoctorRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Doctor, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<Doctor, object>>[]>()
            )).ReturnsAsync(doctor);

            var existingActiveMed = new ActiveMedication
            {
                Id = 100,
                PatientId = patientId,
                DrugName = "Metformin",
                Dosage = "250mg",
                Frequency = "Once daily"
            };

            _mockActiveMedicationsRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<ActiveMedication, bool>>>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ActiveMedication, object>>[]>()
            )).ReturnsAsync(new List<ActiveMedication> { existingActiveMed });

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sessionService.FinalizeTreatmentAsync(sessionId);

            Assert.True(result.IsSuccess);
            Assert.Equal("Completed", session.Status);
            Assert.NotNull(session.FinalizedAt);

            // Existing Metformin active medication should be updated with new dosage/frequency
            Assert.Equal("500mg", existingActiveMed.Dosage);
            Assert.Equal("Twice daily", existingActiveMed.Frequency);
            Assert.Equal(sessionId, existingActiveMed.SourceSessionId);

            // Brand new Aspirin active medication should be added
            _mockActiveMedicationsRepo.Verify(r => r.AddRange(It.Is<List<ActiveMedication>>(
                list => list.Count == 1 && list[0].DrugName == "Aspirin" && list[0].PatientId == patientId
            )), Times.Once);

            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
