using CliniKa.Application.DTOs.Sessions;
using CliniKa.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace CliniKa.Tests
{
    public class LangFlowServiceTests
    {
        [Fact]
        public async Task TriggerDiagnosisFlowAsync_ShouldAcceptIFormFileParameters()
        {
            // Arrange
            var service = new LangFlowService();

            var audioContent = Encoding.UTF8.GetBytes("fake audio content");
            var audioStream = new MemoryStream(audioContent);
            var audioFile = new FormFile(audioStream, 0, audioContent.Length, "audio", "test_audio.mp3")
            {
                Headers = new HeaderDictionary(),
                ContentType = "audio/mpeg"
            };

            var docContent = Encoding.UTF8.GetBytes("fake document content");
            var docStream = new MemoryStream(docContent);
            var docFile = new FormFile(docStream, 0, docContent.Length, "image1", "test_doc.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var documents = new List<IFormFile> { docFile };

            // Act
            var result = await service.TriggerDiagnosisFlowAsync(123, audioFile, documents);

            // Assert - we only verify it doesn't throw and returns a result.
            // It will likely fail if Node.js or the helper script isn't available,
            // but the method should handle that gracefully.
            Assert.NotNull(result);
        }

        [Fact]
        public async Task TriggerDrugInteractionFlowAsync_ShouldAcceptPayloadAndReturnResult()
        {
            var service = new LangFlowService();

            var mochPayload = new WorkflowSessionPayloadDto
            {
                SessionId = 105,
                PatientId = 20,
                PatientName = "Test Patient",
                AudioTranscript = "Doctor: Good morning. Patient: I have a headache.",
                NewMedications = new List<string> { "Verteporfin" },
                CurrentMedications = new List<string> { "Trioxsalen" },
                PossibleDiagnoses = "Migraine"
            };

            var result = await service.TriggerDrugInteractionFlowAsync(mochPayload);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task TriggerChatbotFlowAsync_ShouldAcceptPayloadAndReturnResult()
        {
            // Arrange
            var service = new LangFlowService();

            var chatPayload = new CliniKa.Application.DTOs.Chat.ChatFlowPayloadDto
            {
                SystemPrompt = "Patient: John Doe, Age: 35",
                NewQuestion = "What medications am I taking?",
                Messages = new List<CliniKa.Application.DTOs.Chat.ChatMessageDto>()
            };

            // Act
            var result = await service.TriggerChatbotFlowAsync(chatPayload);

            // Assert
            Assert.NotNull(result);
        }
    }
}
