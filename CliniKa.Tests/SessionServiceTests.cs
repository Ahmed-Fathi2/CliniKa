using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using Xunit;
using CliniKa.Application;
using CliniKa.Application.Abstraction;
using CliniKa.Application.DTOs;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Services;

namespace CliniKa.Tests
{
    public class SessionServiceTests
    {
        private readonly Mock<IGenericRepository<ClinicalSession, int>> _mockSessionRepo;
        private readonly Mock<ILangFlowService> _mockLangFlowService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly SessionService _sessionService;

        public SessionServiceTests()
        {
            _mockSessionRepo = new Mock<IGenericRepository<ClinicalSession, int>>();
            _mockLangFlowService = new Mock<ILangFlowService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            _sessionService = new SessionService(
                _mockSessionRepo.Object,
                _mockLangFlowService.Object,
                _mockUnitOfWork.Object
            );
        }

        [Fact]
        public async Task TranscribeSessionAudioAsync_ShouldReturnNotFound_WhenSessionDoesNotExist()
        {
            // Arrange
            _mockSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ClinicalSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ClinicalSession, object>>[]>()
            )).ReturnsAsync((ClinicalSession?)null);

            var stream = new MemoryStream();

            // Act
            var result = await _sessionService.TranscribeSessionAudioAsync(999, stream, "test.wav");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task TranscribeSessionAudioAsync_ShouldUpdateSessionAndSave_WhenSessionAndLangFlowSucceed()
        {
            // Arrange
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

            var langFlowResult = new LangFlowAudioTranscriptResult
            {
                AudioTranscript = "كحة وسخونة وتاريخ حساسية البنسلين",
                Symptoms = new List<string> { "Cough", "Fever" },
                MentionedMedications = new List<string> { "Voltaren" },
                MentionedHistory = new List<string> { "Kidney Disease" }
            };

            _mockLangFlowService.Setup(s => s.ProcessAudioAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>()
            )).ReturnsAsync(GeneralResult<LangFlowAudioTranscriptResult>.Success(langFlowResult));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            var stream = new MemoryStream();

            // Act
            var result = await _sessionService.TranscribeSessionAudioAsync(sessionId, stream, "test.wav");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(sessionId, result.Data.SessionId);
            Assert.Equal("كحة وسخونة وتاريخ حساسية البنسلين", result.Data.AudioTranscript);
            Assert.Contains("Cough", result.Data.ExtractedSymptoms);
            Assert.Contains("Voltaren", result.Data.MentionedMedications);
            Assert.Contains("Kidney Disease", result.Data.MentionedHistory);

            // Verify session object updated correctly
            Assert.Equal("كحة وسخونة وتاريخ حساسية البنسلين", session.AudioTranscript);
            
            var symptoms = JsonSerializer.Deserialize<List<string>>(session.ExtractedSymptoms!);
            Assert.Contains("Cough", symptoms);

            var meds = JsonSerializer.Deserialize<List<string>>(session.MentionedMedications!);
            Assert.Contains("Voltaren", meds);

            var history = JsonSerializer.Deserialize<List<string>>(session.MentionedHistory!);
            Assert.Contains("Kidney Disease", history);

            // Verify db transaction was saved
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
