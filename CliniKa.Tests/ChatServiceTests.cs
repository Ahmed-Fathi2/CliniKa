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
using Microsoft.AspNetCore.Http;
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
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly ChatService _chatService;

        public ChatServiceTests()
        {
            _mockChatSessionRepo = new Mock<IChatSessionRepository>();
            _mockPatientRepo = new Mock<IPatientRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLangFlowService = new Mock<ILangFlowService>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _mockUnitOfWork.Setup(u => u.ChatSessions).Returns(_mockChatSessionRepo.Object);
            _mockUnitOfWork.Setup(u => u.Patient).Returns(_mockPatientRepo.Object);

            _chatService = new ChatService(_mockUnitOfWork.Object, _mockLangFlowService.Object, _mockHttpContextAccessor.Object);
        }

        #region GetChatHistoryAsync Tests

        [Fact]
        public async Task GetChatHistoryAsync_ShouldReturnMessages_WhenSessionExists()
        {
            // Arrange
            int sessionId = 5;
            int patientId = 1;
            var chatSession = new ChatSession
            {
                Id = sessionId,
                PatientId = patientId,
                Title = "Diabetes Question",
                MessagesJson = "[{\"role\":\"user\",\"content\":\"Hi\",\"timestamp\":\"2026-07-26T12:00:00Z\"}]"
            };

            _mockChatSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync(chatSession);

            // Act
            var result = await _chatService.GetChatHistoryAsync(sessionId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            Assert.Equal("Hi", result.Data[0].Content);
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

        [Fact]
        public async Task GetChatHistoryAsync_ShouldThrowInvalidOperationException_WhenMessagesJsonIsCorrupted()
        {
            // Arrange
            int sessionId = 30;
            var chatSession = new ChatSession
            {
                Id = sessionId,
                PatientId = 1,
                Title = "Corrupted Chat",
                MessagesJson = "{ invalid json content }"
            };

            _mockChatSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync(chatSession);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _chatService.GetChatHistoryAsync(sessionId));
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
        public async Task GetPatientChatsAsync_ShouldReturnEmptyList_WhenPatientHasNoActiveChats()
        {
            // Arrange
            int patientId = 5;
            var patient = new Patient { Id = patientId, FullName = "No Chats Patient" };

            _mockPatientRepo.Setup(p => p.GetByIdAsync(patientId))
                .ReturnsAsync(patient);

            _mockChatSessionRepo.Setup(c => c.GetAllAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync(new List<ChatSession>());

            // Act
            var result = await _chatService.GetPatientChatsAsync(patientId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
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

        #region SendMessageViaAgentAsync Tests

        [Fact]
        public async Task SendMessageViaAgentAsync_ShouldCreateNewSessionAndReturnSuccess_WhenChatSessionIdIsNull()
        {
            // Arrange
            int patientId = 1;
            string userQuestion = "What are the side effects of Metformin?";
            string aiReply = "Common side effects include nausea and diarrhea.";

            var patient = new Patient { Id = patientId, FullName = "Jane Doe" };

            _mockPatientRepo.Setup(p => p.GetByIdAsync(patientId))
                .ReturnsAsync(patient);

            _mockLangFlowService.Setup(l => l.CallLangflowChatbotAgentAsync(userQuestion, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(GeneralResult<string>.Success(aiReply, "Success"));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _chatService.SendMessageViaAgentAsync(patientId, null, userQuestion);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(aiReply, result.Data.Reply);
            _mockChatSessionRepo.Verify(c => c.Add(It.Is<ChatSession>(s =>
                s.PatientId == patientId &&
                s.Title == userQuestion &&
                s.IsActive
            )), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeast(2));
        }

        [Fact]
        public async Task SendMessageViaAgentAsync_ShouldTruncateTitle_WhenInitialMessageIsLongerThan50Chars()
        {
            // Arrange
            int patientId = 1;
            string longMessage = new string('a', 60);

            _mockPatientRepo.Setup(p => p.GetByIdAsync(patientId))
                .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe" });

            _mockLangFlowService.Setup(l => l.CallLangflowChatbotAgentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(GeneralResult<string>.Success("AI reply", "Success"));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _chatService.SendMessageViaAgentAsync(patientId, null, longMessage);

            // Assert
            Assert.True(result.IsSuccess);
            _mockChatSessionRepo.Verify(c => c.Add(It.Is<ChatSession>(s =>
                s.Title.Length == 53 && s.Title.EndsWith("...")
            )), Times.Once);
        }

        [Fact]
        public async Task SendMessageViaAgentAsync_ShouldAppendToExistingSession_WhenChatSessionIdIsValid()
        {
            // Arrange
            int patientId = 1;
            int sessionId = 10;
            string userQuestion = "Follow-up question";
            string aiReply = "Follow-up AI answer";

            var existingSession = new ChatSession
            {
                Id = sessionId,
                PatientId = patientId,
                Title = "Existing Chat",
                MessagesJson = "[{\"role\":\"user\",\"content\":\"first question\",\"timestamp\":\"2026-07-26T10:00:00Z\"},{\"role\":\"assistant\",\"content\":\"first answer\",\"timestamp\":\"2026-07-26T10:00:05Z\"}]",
                IsActive = true
            };

            _mockChatSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync(existingSession);

            _mockLangFlowService.Setup(l => l.CallLangflowChatbotAgentAsync(userQuestion, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(GeneralResult<string>.Success(aiReply, "Success"));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _chatService.SendMessageViaAgentAsync(patientId, sessionId, userQuestion);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(sessionId, result.Data.ChatSessionId);
            Assert.Equal(aiReply, result.Data.Reply);
            Assert.Contains("first question", existingSession.MessagesJson);
            Assert.Contains(userQuestion, existingSession.MessagesJson);
            Assert.Contains(aiReply, existingSession.MessagesJson);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SendMessageViaAgentAsync_ShouldReturnNotFound_WhenPatientDoesNotExist()
        {
            // Arrange
            int patientId = 999;
            _mockPatientRepo.Setup(p => p.GetByIdAsync(patientId))
                .ReturnsAsync((Patient?)null);

            // Act
            var result = await _chatService.SendMessageViaAgentAsync(patientId, null, "Hello");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
            Assert.Equal("Patient not found.", result.Message);
        }

        [Fact]
        public async Task SendMessageViaAgentAsync_ShouldReturnNotFound_WhenChatSessionDoesNotExist()
        {
            // Arrange
            int patientId = 1;
            int invalidSessionId = 888;

            _mockChatSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync((ChatSession?)null);

            // Act
            var result = await _chatService.SendMessageViaAgentAsync(patientId, invalidSessionId, "Hello");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.NotFound, result.Status);
        }

        [Fact]
        public async Task SendMessageViaAgentAsync_ShouldReturnFailure_WhenLangflowAgentFails()
        {
            // Arrange
            int patientId = 1;
            int sessionId = 15;

            var existingSession = new ChatSession
            {
                Id = sessionId,
                PatientId = patientId,
                Title = "Test Chat",
                MessagesJson = "[]",
                IsActive = true
            };

            _mockChatSessionRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<ChatSession, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<Expression<Func<ChatSession, object>>[]>()
            )).ReturnsAsync(existingSession);

            _mockLangFlowService.Setup(l => l.CallLangflowChatbotAgentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(GeneralResult<string>.Failure("Agent timed out."));

            // Act
            var result = await _chatService.SendMessageViaAgentAsync(patientId, sessionId, "Hello");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Agent timed out.", result.Message);
        }

        [Fact]
        public async Task SendMessageViaAgentAsync_ShouldExtractJwtTokenFromAuthorizationHeader()
        {
            // Arrange
            int patientId = 1;
            string token = "my-secret-jwt-token";
            string userQuestion = "Check my meds";

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = $"Bearer {token}";

            _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

            _mockPatientRepo.Setup(p => p.GetByIdAsync(patientId))
                .ReturnsAsync(new Patient { Id = patientId, FullName = "Jane Doe" });

            string? passedToken = null;
            _mockLangFlowService.Setup(l => l.CallLangflowChatbotAgentAsync(userQuestion, It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((msg, sessId, jwt) => passedToken = jwt)
                .ReturnsAsync(GeneralResult<string>.Success("AI answer", "Success"));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _chatService.SendMessageViaAgentAsync(patientId, null, userQuestion);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(token, passedToken);
        }

        #endregion
    }
}

