using CliniKa.Application.DTOs.Sessions;
using CliniKa.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CliniKa.Tests
{
    public class LangFlowServiceTests
    {
        private static LangFlowService CreateService()
        {
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

            var configValues = new Dictionary<string, string?>
            {
                { "LangFlow:AgentFlowUrl", "http://localhost:7860/api/v1/run/test-flow-id" },
                { "LangFlow:AgentApiKey", "test-api-key" }
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            return new LangFlowService(mockHttpClientFactory.Object, configuration);
        }

        private static LangFlowService CreateServiceWithMockHttp(
            HttpStatusCode statusCode,
            string responseContent,
            Action<HttpRequestMessage>? onRequest = null,
            string? apiKey = "test-api-key")
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) => onRequest?.Invoke(req))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var configValues = new Dictionary<string, string?>
            {
                { "LangFlow:AgentFlowUrl", "http://localhost:7860/api/v1/run/test-flow-id" },
                { "LangFlow:AgentApiKey", apiKey }
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            return new LangFlowService(mockHttpClientFactory.Object, configuration);
        }

        [Fact]
        public async Task TriggerDiagnosisFlowAsync_ShouldAcceptIFormFileParameters()
        {
            // Arrange
            var service = CreateService();

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

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task TriggerDrugInteractionFlowAsync_ShouldAcceptPayloadAndReturnResult()
        {
            var service = CreateService();

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

        #region CallLangflowChatbotAgentAsync Tests

        [Fact]
        public async Task CallLangflowChatbotAgentAsync_ShouldReturnSuccess_WhenLangflowReturnsStandardNestedResponse()
        {
            // Arrange
            string expectedReply = "Caffeine can act as a mild **diuretic** and stimulates colon muscles.";
            string langflowJsonResponse = @"
            {
                ""outputs"": [
                    {
                        ""outputs"": [
                            {
                                ""results"": {
                                    ""message"": {
                                        ""text"": """ + expectedReply + @"""
                                    }
                                }
                            }
                        ]
                    }
                ]
            }";

            var service = CreateServiceWithMockHttp(HttpStatusCode.OK, langflowJsonResponse);

            // Act
            var result = await service.CallLangflowChatbotAgentAsync("Why does coffee make me poop?", "clinika-chat-1", "jwt-token-123");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedReply, result.Data);
        }

        [Fact]
        public async Task CallLangflowChatbotAgentAsync_ShouldReturnSuccess_WhenLangflowReturnsFallbackKeys()
        {
            // Arrange
            string expectedReply = "Take your medication with meals.";
            string langflowJsonResponse = @"{ ""reply"": """ + expectedReply + @""" }";

            var service = CreateServiceWithMockHttp(HttpStatusCode.OK, langflowJsonResponse);

            // Act
            var result = await service.CallLangflowChatbotAgentAsync("When to take meds?", "clinika-chat-2", "jwt-token-456");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedReply, result.Data);
        }

        [Fact]
        public async Task CallLangflowChatbotAgentAsync_ShouldBuildCorrectRequestPayloadAndHeaders()
        {
            // Arrange
            string userMessage = "What are my allergies?";
            string sessionId = "clinika-chat-42";
            string jwtToken = "patient-jwt-abc";
            string configuredApiKey = "secret-langflow-key";

            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;

            var service = CreateServiceWithMockHttp(
                HttpStatusCode.OK,
                @"{ ""result"": ""You are allergic to Penicillin."" }",
                onRequest: req =>
                {
                    capturedRequest = req;
                    capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                },
                apiKey: configuredApiKey);

            // Act
            var result = await service.CallLangflowChatbotAgentAsync(userMessage, sessionId, jwtToken);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(capturedRequest);
            Assert.Equal(HttpMethod.Post, capturedRequest.Method);
            Assert.True(capturedRequest.Headers.Contains("x-api-key"));
            Assert.Equal(configuredApiKey, capturedRequest.Headers.GetValues("x-api-key").First());

            // Validate JSON body structure
            Assert.NotNull(capturedBody);
            using var doc = JsonDocument.Parse(capturedBody);
            var root = doc.RootElement;

            Assert.Equal("chat", root.GetProperty("output_type").GetString());
            Assert.Equal("chat", root.GetProperty("input_type").GetString());
            Assert.Equal(userMessage, root.GetProperty("input_value").GetString());
            Assert.Equal(sessionId, root.GetProperty("session_id").GetString());

            var tweaks = root.GetProperty("tweaks");
            var customComponent = tweaks.GetProperty("CustomComponent-25Mo5");
            Assert.Equal(jwtToken, customComponent.GetProperty("auth_token").GetString());
        }

        [Fact]
        public async Task CallLangflowChatbotAgentAsync_ShouldReturnFailure_WhenLangflowReturnsHttpError()
        {
            // Arrange
            string errorResponse = @"{ ""detail"": ""Flow execution failed due to invalid token."" }";
            var service = CreateServiceWithMockHttp(HttpStatusCode.InternalServerError, errorResponse);

            // Act
            var result = await service.CallLangflowChatbotAgentAsync("Hello", "clinika-chat-1", "invalid-token");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("500", result.Message);
            Assert.Contains("Flow execution failed", result.Message);
        }

        [Fact]
        public async Task CallLangflowChatbotAgentAsync_ShouldReturnFailure_WhenResponseCannotBeParsed()
        {
            // Arrange - invalid JSON response that returns null
            var service = CreateServiceWithMockHttp(HttpStatusCode.OK, "{ invalid json structure }");

            // Act
            var result = await service.CallLangflowChatbotAgentAsync("Hello", "clinika-chat-1", "token");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Failed to extract reply from Langflow Agent response.", result.Message);
        }

        [Fact]
        public async Task CallLangflowChatbotAgentAsync_ShouldHandleHttpRequestExceptionGracefully()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var httpClient = new HttpClient(mockHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var configValues = new Dictionary<string, string?>
            {
                { "LangFlow:AgentFlowUrl", "http://localhost:7860/api/v1/run/test-flow-id" },
                { "LangFlow:AgentApiKey", "test-api-key" }
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            var service = new LangFlowService(mockHttpClientFactory.Object, configuration);

            // Act
            var result = await service.CallLangflowChatbotAgentAsync("Hello", "clinika-chat-1", "token");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("HTTP error calling Langflow Agent", result.Message);
            Assert.Contains("Connection refused", result.Message);
        }

        #endregion
    }
}
