using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using CliniKa.Infrastructure.Services;

namespace CliniKa.Tests
{
    public class LangFlowServiceTests
    {
        [Fact]
        public async Task TriggerDiagnosisFlowAsync_ShouldReturnSuccess_WhenApiCallIsSuccessful()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            
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
                    Content = new StringContent("success", Encoding.UTF8, "text/plain")
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var service = new LangFlowService(httpClient);

            // Create temporary files to avoid File.Exists returning false
            var tempAudio = Path.GetTempFileName();
            var tempDoc = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(tempAudio, "fake audio content");
                await File.WriteAllTextAsync(tempDoc, "fake document content");

                var documents = new List<string> { tempDoc };

                // Act
                var result = await service.TriggerDiagnosisFlowAsync(123, tempAudio, documents);

                // Assert
                Assert.True(result.IsSuccess);
                Assert.Equal("success", result.Data);
            }
            finally
            {
                if (File.Exists(tempAudio)) File.Delete(tempAudio);
                if (File.Exists(tempDoc)) File.Delete(tempDoc);
            }
        }
    }
}
