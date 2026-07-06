using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using CliniKa.Application;
using CliniKa.Application.Abstraction;
using CliniKa.Application.DTOs;
using CliniKa.Infrastructure.Options;

namespace CliniKa.Infrastructure.Services
{
    public class LangFlowService : ILangFlowService
    {
        private readonly HttpClient _httpClient;
        private readonly LangFlowOptions _options;

        public LangFlowService(HttpClient httpClient, IOptions<LangFlowOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<GeneralResult<LangFlowAudioTranscriptResult>> ProcessAudioAsync(Stream audioStream, string fileName)
        {
            try
            {
                var baseUrl = _options.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/api/v1/run/{_options.AudioFlowId}?stream=false";

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

                var multipartContent = new MultipartFormDataContent();
                var streamContent = new StreamContent(audioStream);
                
                // Determine appropriate content type based on extension
                var ext = Path.GetExtension(fileName).ToLower();
                var contentType = ext switch
                {
                    ".mp3" => "audio/mpeg",
                    ".m4a" => "audio/mp4",
                    ".wav" => "audio/wav",
                    _ => "application/octet-stream"
                };
                
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                multipartContent.Add(streamContent, "file", fileName);
                request.Content = multipartContent;

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return GeneralResult<LangFlowAudioTranscriptResult>.Failure(
                        $"LangFlow request failed with status code {response.StatusCode}. Details: {errorResponse}");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var langFlowResponse = JsonSerializer.Deserialize<LangFlowResponse>(responseString, deserializeOptions);
                var rawText = langFlowResponse?.Outputs?[0]?.Outputs?[0]?.Results?.Message?.Text;

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    return GeneralResult<LangFlowAudioTranscriptResult>.Failure("LangFlow response message text is empty.");
                }

                // Deserialise the inner JSON string returned from the message text
                var result = JsonSerializer.Deserialize<LangFlowAudioTranscriptResult>(rawText, deserializeOptions);

                if (result == null)
                {
                    return GeneralResult<LangFlowAudioTranscriptResult>.Failure("Failed to deserialize the transcript and entities from LangFlow response.");
                }

                return GeneralResult<LangFlowAudioTranscriptResult>.Success(result, "Audio transcribed and entities extracted successfully.");
            }
            catch (Exception ex)
            {
                return GeneralResult<LangFlowAudioTranscriptResult>.Failure($"An error occurred while communicating with LangFlow: {ex.Message}");
            }
        }
    }
}
