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
            _mockLangFlowService = new Mock<ILangFlowService>();
            _mockFileService = new Mock<IFileService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _mockUnitOfWork.Setup(u => u.ClinicalSessions).Returns(_mockSessionRepo.Object);
            _mockUnitOfWork.Setup(u => u.Doctors).Returns(_mockDoctorRepo.Object);
            _mockUnitOfWork.Setup(u => u.PrescribedMedications).Returns(_mockPrescribedRepo.Object);

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
        public async Task SaveDiagnosisCallbackResultAsync_ShouldUpdateSessionAndAiReport_WhenSuccessful()
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

            var callbackResult = new LangFlowDiagnosisResult
            {
                Status = 200,
                Body = new LangFlowDiagnosisResultBody
                {
                    Transcript = "Doctor: Hello. Patient: Hello doctor.",
                    Analysis = "Some analysis",
                    ChiefComplaint = "Fever and cough",
                    HistoryOfPresentIllness = "Feeling unwell for 4 days",
                    PossibleDiagnosis = "Viral infection",
                    ImagesReceived = 1
                }
            };

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var result = await _sessionService.SaveDiagnosisCallbackResultAsync(sessionId, callbackResult);

            Assert.True(result.IsSuccess);
            Assert.Equal("Completed", session.Status);
            Assert.Equal("Doctor: Hello. Patient: Hello doctor.", session.AudioTranscript);
            Assert.Equal("Fever and cough", session.ExtractedSymptoms);

            Assert.NotNull(session.SessionAiReport);
            Assert.Equal("Feeling unwell for 4 days", session.SessionAiReport.PatientSummary);
            Assert.Equal("Viral infection", session.SessionAiReport.PossibleDiagnoses);

            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
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
        public async Task UpdatePrescribedMedicationsAsync_ShouldDeleteExistingAndAddNewMedications_WhenSuccessful()
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
                new PrescribedMedication { Id = 1, SessionId = sessionId, DrugName = "OldMed" }
            };

            _mockPrescribedRepo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<PrescribedMedication, bool>>>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<PrescribedMedication, object>>[]>()
            )).ReturnsAsync(existingMeds);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var newMeds = new List<PrescribeMedicationRequest>
            {
                new PrescribeMedicationRequest { DrugName = "Panadol", Dosage = "1000mg", Frequency = "Twice daily" }
            };

            var result = await _sessionService.UpdatePrescribedMedicationsAsync(sessionId, newMeds);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Data!);
            Assert.Equal("Panadol", result.Data![0].DrugName);
            Assert.Equal("1000mg", result.Data![0].Dosage);

            _mockPrescribedRepo.Verify(r => r.DeleteRange(existingMeds), Times.Once);
            _mockPrescribedRepo.Verify(r => r.AddRange(It.Is<List<PrescribedMedication>>(l => l.Count == 1 && l[0].DrugName == "Panadol")), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
