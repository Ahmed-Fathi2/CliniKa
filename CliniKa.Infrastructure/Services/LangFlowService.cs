using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;

namespace CliniKa.Infrastructure.Services
{
    public class LangFlowService : ILangFlowService
    {
        private readonly HttpClient _httpClient;

        public LangFlowService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GeneralResult<string>> TriggerDiagnosisFlowAsync(
            int sessionId,
            string audioFilePath,
            List<string> documentFilePaths)
        {
            try
            {
                var url = "https://cloud.activepieces.com/api/v1/webhooks/p1CEPLFpP2z8qBAhZA48A/sync";

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                var multipartContent = new MultipartFormDataContent();

                // Add sessionId
                multipartContent.Add(new StringContent(sessionId.ToString()), "sessionId");

                // Add audio file (under key 'file')
                if (File.Exists(audioFilePath))
                {
                    var audioFileName = Path.GetFileName(audioFilePath);
                    var audioBytes = await File.ReadAllBytesAsync(audioFilePath);
                    
                    var audioContent = new ByteArrayContent(audioBytes);
                    audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetContentType(audioFileName));
                    multipartContent.Add(audioContent, "file", audioFileName);
                }

                // Add documents (under keys image1, image2, image3...)
                if (documentFilePaths != null)
                {
                    for (int i = 0; i < documentFilePaths.Count; i++)
                    {
                        var path = documentFilePaths[i];
                        if (File.Exists(path))
                        {
                            var fileName = Path.GetFileName(path);
                            var fileBytes = await File.ReadAllBytesAsync(path);

                            var docContent = new ByteArrayContent(fileBytes);
                            docContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetContentType(fileName));
                            
                            var key = $"image{i + 1}";
                            multipartContent.Add(docContent, key, fileName);
                        }
                    }
                }

                request.Content = multipartContent;

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    return GeneralResult<string>.Failure($"Activepieces webhook returned error status code: {response.StatusCode}. Details: {errorDetails}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return GeneralResult<string>.Success(responseContent, "Activepieces webhook triggered successfully.");
            }
            catch (Exception ex)
            {
                return GeneralResult<string>.Failure($"Exception occurred while triggering Activepieces flow: {ex.Message}");
            }
        }

        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".mp3" => "audio/mpeg",
                ".m4a" => "audio/mp4",
                ".wav" => "audio/wav",
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}
