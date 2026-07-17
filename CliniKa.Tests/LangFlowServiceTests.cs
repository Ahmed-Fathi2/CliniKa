using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;
using CliniKa.Infrastructure.Services;

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
    }
}
