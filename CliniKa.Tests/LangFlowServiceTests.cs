using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;
using CliniKa.Infrastructure.Options;
using CliniKa.Infrastructure.Services;

namespace CliniKa.Tests
{
    public class LangFlowServiceTests
    {
        [Fact]
        public async Task ProcessAudioAsync_ShouldReturnSuccess_WhenApiCallIsSuccessful()
        {
            // Arrange
            var options = new LangFlowOptions
            {
                BaseUrl = "https://api.langflow.store",
                ApiKey = "test-api-key",
                AudioFlowId = "test-flow-id",
                VisionFlowId = "vision-id",
                SafetyFlowId = "safety-id"
            };
            var mockOptions = new Mock<IOptions<LangFlowOptions>>();
            mockOptions.Setup(o => o.Value).Returns(options);

            // Mocking HttpMessageHandler
            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            
            // Raw JSON response matching LangFlow response format
            var rawLangFlowResponse = @"{
                ""outputs"": [
                  {
                    ""outputs"": [
                      {
                        ""results"": {
                          ""message"": {
                            ""text"": ""{\""AudioTranscript\"": \""كحة وسخونة\"", \""Symptoms\"": [\""Cough\"", \""Fever\""], \""MentionedMedications\"": [\""Panadol\""], \""MentionedHistory\"": []}""
                          }
                        }
                      }
                    ]
                  }
                ]
            }";

            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(rawLangFlowResponse, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var service = new LangFlowService(httpClient, mockOptions.Object);

            var audioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));
            var fileName = "test.wav";

            // Act
            var result = await service.ProcessAudioAsync(audioStream, fileName);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("كحة وسخونة", result.Data.AudioTranscript);
            Assert.Contains("Cough", result.Data.Symptoms);
            Assert.Contains("Panadol", result.Data.MentionedMedications);
        }
    }
}
