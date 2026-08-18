using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Chat;
using CliniKa.Application.DTOs.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CliniKa.Infrastructure.Services
{
    public class LangFlowService : ILangFlowService
    {
        private const string WebhookUrl = "https://cloud.activepieces.com/api/v1/webhooks/p1CEPLFpP2z8qBAhZA48A";
        private const string DrugInteractionWebhookUrl = "https://cloud.activepieces.com/api/v1/webhooks/irtY6rMKe4Mb1fVjwTuGv/sync";
        // TODO: Replace with your actual ActivePieces chatbot flow webhook URL
        private const string ChatbotWebhookUrl = "https://matrimony-valiant-unedited.ngrok-free.dev/patient-chat";
        private const string HelperScriptName = "langflow_helper.js";
        private const string HelperInteractionsScriptName = "langflow_trigger_interaction_json_helper.js";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _agentFlowUrl;
        private readonly string _agentApiKey;

        public LangFlowService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _agentFlowUrl = configuration["LangFlow:AgentFlowUrl"]
                ?? throw new InvalidOperationException("LangFlow:AgentFlowUrl is not configured in appsettings.");
            _agentApiKey = configuration["LangFlow:AgentApiKey"] ?? string.Empty;
        }

        public async Task<GeneralResult<string>> TriggerDiagnosisFlowAsync(
        int sessionId,
        IFormFile audioFile,
        List<IFormFile>? documentFiles)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            try
            {
                Console.WriteLine($"[LangFlowService] Triggering Activepieces via HttpClient for Session {sessionId}...");

                using var httpClient = new HttpClient();
                using var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(sessionId.ToString()), "sessionId");

                var audioStream = audioFile.OpenReadStream();
                var audioContent = new StreamContent(audioStream);
                formData.Add(audioContent, "audio", audioFile.FileName);

                var streamsToDispose = new List<Stream>();
                if (documentFiles != null)
                {
                    int docIndex = 1;
                    foreach (var doc in documentFiles)
                    {
                        if (doc.Length == 0) continue;

                        var docStream = doc.OpenReadStream();
                        streamsToDispose.Add(docStream);

                        var docContent = new StreamContent(docStream);
                        formData.Add(docContent, $"document_{docIndex}", doc.FileName);

                        docIndex++;
                    }
                }

                var response = await httpClient.PostAsync(WebhookUrl, formData, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

                audioStream.Dispose();
                foreach (var stream in streamsToDispose)
                {
                    stream.Dispose();
                }

                Console.WriteLine($"[LangFlowService] Activepieces responded with status {(int)response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    return GeneralResult<string>.Success(responseBody, "Activepieces flow triggered successfully.");
                }

                return GeneralResult<string>.Failure($"Activepieces returned status {(int)response.StatusCode}. Details: {responseBody}");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[LangFlowService] Execution aborted due to target timeout constraint.");
                return GeneralResult<string>.Failure("External workflow orchestration process timed out.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LangFlowService] Exception: {ex.Message}");
                return GeneralResult<string>.Failure($"Exception triggering Activepieces flow: {ex.Message}");
            }
        }

        private static async Task SaveFormFileToTempAsync(IFormFile file, string path)
        {
            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        private static string? ResolveHelperScriptPath(string scriptName)
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, scriptName),
                Path.Combine(Directory.GetCurrentDirectory(), scriptName),
                Path.Combine(Directory.GetCurrentDirectory(), "CliniKa.Presentation", scriptName)
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path)) return path;
            }

            return null;
        }

        public async Task<GeneralResult<object>> TriggerDrugInteractionFlowAsync(WorkflowSessionPayloadDto payload)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            try
            {
                Console.WriteLine($"[LangFlowService] Triggering Drug Interaction Flow via HttpClient for Session {payload.SessionId}...");

                using var httpClient = new HttpClient();

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(DrugInteractionWebhookUrl, content, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

                Console.WriteLine($"[LangFlowService] Drug Interaction Flow responded with status {(int)response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var parsedBody = string.IsNullOrWhiteSpace(responseBody)
                        ? null
                        : JsonSerializer.Deserialize<object>(responseBody);

                    return GeneralResult<object>.Success(parsedBody, "Triggered successfully via HttpClient");
                }

                return GeneralResult<object>.Failure($"Status {(int)response.StatusCode}: {responseBody}");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[LangFlowService] Drug Interaction flow timed out.");
                return GeneralResult<object>.Failure("Drug Interaction flow timed out.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LangFlowService] Exception: {ex.Message}");
                return GeneralResult<object>.Failure($"Exception: {ex.Message}");
            }
        }


        /// <summary>
        /// Calls the Langflow Agent API directly via HttpClient.
        /// The agent handles chat memory internally via session_id and retrieves
        /// patient medical data using its own tool (get_patient_medical_profile) with the provided JWT.
        /// </summary>
        public async Task<GeneralResult<string>> CallLangflowChatbotAgentAsync(string userMessage, string sessionId, string jwtToken)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));

            try
            {
                Console.WriteLine($"[LangFlowService] Calling Langflow Agent (session: {sessionId})...");

                var httpClient = _httpClientFactory.CreateClient();

                // Build the exact payload matching the Langflow API format
                var payload = new
                {
                    output_type = "chat",
                    input_type = "chat",
                    input_value = userMessage,
                    session_id = sessionId,
                    tweaks = new Dictionary<string, object>
                    {
                        ["CustomComponent-25Mo5"] = new Dictionary<string, string>
                        {
                            ["auth_token"] = jwtToken
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Set x-api-key header if configured
                var request = new HttpRequestMessage(HttpMethod.Post, _agentFlowUrl)
                {
                    Content = content
                };

                if (!string.IsNullOrEmpty(_agentApiKey))
                {
                    request.Headers.Add("x-api-key", _agentApiKey);
                }

                var response = await httpClient.SendAsync(request, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

                Console.WriteLine($"[LangFlowService] Langflow Agent responded with status {(int)response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[LangFlowService] Agent error response: {responseBody}");
                    return GeneralResult<string>.Failure($"Langflow Agent returned status {(int)response.StatusCode}: {responseBody}");
                }

                // Parse Langflow response to extract the AI reply text
                // Expected structure: { "outputs": [{ "outputs": [{ "results": { "message": { "text": "..." } } }] }] }
                var aiReply = ExtractLangflowAgentReply(responseBody);
                if (aiReply == null)
                {
                    Console.WriteLine($"[LangFlowService] Could not extract reply from response: {responseBody}");
                    return GeneralResult<string>.Failure("Failed to extract reply from Langflow Agent response.");
                }

                return GeneralResult<string>.Success(aiReply, "Langflow Agent responded successfully.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[LangFlowService] Langflow Agent call timed out.");
                return GeneralResult<string>.Failure("Langflow Agent call timed out after 120 seconds.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[LangFlowService] HTTP error calling Langflow Agent: {ex.Message}");
                return GeneralResult<string>.Failure($"HTTP error calling Langflow Agent: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LangFlowService] Exception calling Langflow Agent: {ex.Message}");
                return GeneralResult<string>.Failure($"Exception calling Langflow Agent: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts the AI reply text from the Langflow Agent API response JSON.
        /// Tries the standard nested structure first, then falls back to simpler paths.
        /// </summary>
        private static string? ExtractLangflowAgentReply(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Standard Langflow response: outputs[0].outputs[0].results.message.text
                if (root.TryGetProperty("outputs", out var outputsArray)
                    && outputsArray.GetArrayLength() > 0)
                {
                    var firstOutput = outputsArray[0];
                    if (firstOutput.TryGetProperty("outputs", out var innerOutputsArray)
                        && innerOutputsArray.GetArrayLength() > 0)
                    {
                        var innerFirst = innerOutputsArray[0];
                        if (innerFirst.TryGetProperty("results", out var results)
                            && results.TryGetProperty("message", out var message)
                            && message.TryGetProperty("text", out var text))
                        {
                            return text.GetString();
                        }
                    }
                }

                // Fallback: try common top-level keys
                foreach (var key in new[] { "result", "response", "message", "text", "reply", "answer" })
                {
                    if (root.TryGetProperty(key, out var prop))
                    {
                        if (prop.ValueKind == JsonValueKind.String)
                            return prop.GetString();
                        if (prop.TryGetProperty("text", out var innerText))
                            return innerText.GetString();
                    }
                }

                // Last resort: return raw JSON
                return responseJson;
            }
            catch
            {
                return null;
            }
        }
    }
}
