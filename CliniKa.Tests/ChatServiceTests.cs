using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Chat;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Services;
using Moq;
using Xunit;

namespace CliniKa.Tests
{
    public class ChatServiceTests
    {
        private readonly Mock<IChatSessionRepository> _mockChatSessionRepo;
        private readonly Mock<IPatientRepository> _mockPatientRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILangFlowService> _mockLangFlowService;
        private readonly ChatService _chatService;

        public ChatServiceTests()
        {
            _mockChatSessionRepo = new Mock<IChatSessionRepository>();
            _mockPatientRepo = new Mock<IPatientRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLangFlowService = new Mock<ILangFlowService>();

            _mockUnitOfWork.Setup(u => u.ChatSessions).Returns(_mockChatSessionRepo.Object);
            _mockUnitOfWork.Setup(u => u.Patient).Returns(_mockPatientRepo.Object);

            _chatService = new ChatService(_mockUnitOfWork.Object, _mockLangFlowService.Object);
        }

        #region StartChatAsync Tests

        [Fact]
        public async Task StartChatAsync_ShouldReturnSuccess_WhenPatientExists()
        {
            // Arrange
            int patientId = 1;
            var patient = new Patient
            {
                Id = patientId,
                FullName = "John Doe",
                Gender = "Male",
                BloodType = "O+",
                DateOfBirth = new DateTime(1990, 1, 1),
                ActiveMedications = new List<ActiveMedication>
                {
                    new ActiveMedication { DrugName = "Metformin", Dosage = "500mg", Frequency = "Twice daily", StartDate = DateTime.UtcNow }
                },
                PatientDiseases = new List<PatientDisease>(),
                PatientAllergies = new List<PatientAllergy>()
            };

            _mockPatientRepo.Setup(p => p.GetPatientWithIncludesAsync(patientId))
                .ReturnsAsync(patient);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _chatService.StartChatAsync(patientId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.False(string.IsNullOrEmpty(result.Data.WelcomeMessage));
            _mockChatSessionRepo.Verify(c => c.Add(It.Is<ChatSession>(s => s.PatientId == patientId)), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task StartChatAsync_ShouldReturnNotFound_WhenPatientDoesNotExist()
        {
            // Arrange
            int patientId = 999;
            _mockPatientRepo.Setup(p => p.GetPatientWithIncludesAsync(patientId))
                .ReturnsAsync((Patient?)null);

            // Act
            var result = await _chatService.StartChatAsync(patientId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        #endregion

        #region SendMessageAsync Tests

        [Fact]
        public async Task SendMessageAsync_ShouldReturnSuccess_WhenSessionExistsAndFlowSucceeds()
        {
            // Arrange
            int sessionId = 10;
            string userQuestion = "What are the side effects of Metformin?";
            string aiReply = "Metformin can cause nausea or upset stomach.";

            var chatSession = new ChatSession
            {
                Id = sessionId,
                PatientId = 1,
                Title = "New Conversation",
                SystemPrompt = "Patient: John Doe, Active Meds: Metformin",
                MessagesJson = "[]",
                IsActive = true
            };

            _mockChatSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync(chatSession);

            _mockLangFlowService.Setup(l => l.TriggerChatbotFlowAsync(It.IsAny<ChatFlowPayloadDto>()))
                .ReturnsAsync(GeneralResult<string>.Success(aiReply, "Success"));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _chatService.SendMessageAsync(sessionId, userQuestion);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(aiReply, result.Data.Reply);
            Assert.Contains(userQuestion, chatSession.MessagesJson);
            Assert.Contains(aiReply, chatSession.MessagesJson);
            Assert.Equal(userQuestion, chatSession.Title); // Auto-title set from first question
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldReturnNotFound_WhenSessionDoesNotExist()
        {
            // Arrange
            _mockChatSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync((ChatSession?)null);

            // Act
            var result = await _chatService.SendMessageAsync(10, "Hello");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldReturnFailure_WhenFlowFails()
        {
            // Arrange
            int sessionId = 10;
            var chatSession = new ChatSession
            {
                Id = sessionId,
                PatientId = 1,
                Title = "General Chat",
                SystemPrompt = "System Prompt",
                MessagesJson = "[]",
                IsActive = true
            };

            _mockChatSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync(chatSession);

            _mockLangFlowService.Setup(l => l.TriggerChatbotFlowAsync(It.IsAny<ChatFlowPayloadDto>()))
                .ReturnsAsync(GeneralResult<string>.Failure("AI Service Unavailable"));

            // Act
            var result = await _chatService.SendMessageAsync(sessionId, "Hello");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("AI Service Unavailable", result.Message);
        }

        #endregion

        #region GetChatHistoryAsync Tests

        [Fact]
        public async Task GetChatHistoryAsync_ShouldReturnMessagesAndRefreshSystemPrompt_WhenSessionExists()
        {
            // Arrange
            int sessionId = 5;
            int patientId = 1;
            var chatSession = new ChatSession
            {
                Id = sessionId,
                PatientId = patientId,
                Title = "Diabetes Question",
                SystemPrompt = "Old System Prompt",
                MessagesJson = "[{\"role\":\"user\",\"content\":\"Hi\",\"timestamp\":\"2026-07-26T12:00:00Z\"}]"
            };

            var patient = new Patient
            {
                Id = patientId,
                FullName = "Jane Doe",
                ActiveMedications = new List<ActiveMedication>(),
                PatientDiseases = new List<PatientDisease>(),
                PatientAllergies = new List<PatientAllergy>()
            };

            _mockChatSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync(chatSession);

            _mockPatientRepo.Setup(p => p.GetPatientWithIncludesAsync(patientId))
                .ReturnsAsync(patient);

            // Act
            var result = await _chatService.GetChatHistoryAsync(sessionId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            Assert.Equal("Hi", result.Data[0].Content);
            Assert.Contains("Jane Doe", chatSession.SystemPrompt); // Refreshed prompt contains patient name
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetChatHistoryAsync_ShouldReturnNotFound_WhenSessionDoesNotExist()
        {
            // Arrange
            _mockChatSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync((ChatSession?)null);

            // Act
            var result = await _chatService.GetChatHistoryAsync(99);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        #endregion

        #region GetPatientChatsAsync Tests

        [Fact]
        public async Task GetPatientChatsAsync_ShouldReturnSummaries_WhenPatientExists()
        {
            // Arrange
            int patientId = 1;
            var patient = new Patient { Id = patientId, FullName = "Jane Doe" };

            var chat1 = new ChatSession
            {
                Id = 101,
                PatientId = patientId,
                Title = "Medication question",
                LastMessageAt = DateTime.UtcNow,
                MessagesJson = "[{\"role\":\"user\",\"content\":\"q1\"},{\"role\":\"assistant\",\"content\":\"a1\"}]",
                IsActive = true
            };

            var chat2 = new ChatSession
            {
                Id = 102,
                PatientId = patientId,
                Title = "Allergy question",
                LastMessageAt = DateTime.UtcNow.AddHours(-2),
                MessagesJson = "[{\"role\":\"user\",\"content\":\"q2\"}]",
                IsActive = true
            };

            _mockPatientRepo.Setup(p => p.GetByIdAsync(patientId))
                .ReturnsAsync(patient);

            _mockChatSessionRepo.Setup(c => c.GetAllAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync(new List<ChatSession> { chat1, chat2 });

            // Act
            var result = await _chatService.GetPatientChatsAsync(patientId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);
            Assert.Equal("Medication question", result.Data[0].Title);
            Assert.Equal(2, result.Data[0].MessageCount);
            Assert.Equal(1, result.Data[1].MessageCount);
        }

        [Fact]
        public async Task GetPatientChatsAsync_ShouldReturnNotFound_WhenPatientDoesNotExist()
        {
            // Arrange
            int patientId = 999;
            _mockPatientRepo.Setup(p => p.GetByIdAsync(patientId))
                .ReturnsAsync((Patient?)null);

            // Act
            var result = await _chatService.GetPatientChatsAsync(patientId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        #endregion
    }
}
