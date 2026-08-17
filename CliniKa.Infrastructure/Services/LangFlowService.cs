using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Chat;
using CliniKa.Application.DTOs.Sessions;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
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
        private const string ChatbotHelperScriptName = "chatbot_flow_json_helper.js";

        public LangFlowService()
        {
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
        /// Sends system prompt + conversation history to the ActivePieces chatbot flow.
        /// Uses the same Node.js helper pattern (JSON payload via temp file) for SSL compatibility.
        /// </summary>
        public async Task<GeneralResult<string>> TriggerChatbotFlowAsync(ChatFlowPayloadDto payload)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"chat_payload_{Guid.NewGuid()}.json");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(240));

            try
            {
                Console.WriteLine($"[LangFlowService] Triggering Chatbot Flow...");

                var jsonPayload = JsonSerializer.Serialize(payload);
                await File.WriteAllTextAsync(tempFilePath, jsonPayload, new System.Text.UTF8Encoding(false), cts.Token);

                var scriptPath = ResolveHelperScriptPath(ChatbotHelperScriptName);
                if (scriptPath == null)
                    return GeneralResult<string>.Failure($"Could not find {ChatbotHelperScriptName}.");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                startInfo.ArgumentList.Add(scriptPath);
                startInfo.ArgumentList.Add(ChatbotWebhookUrl);
                startInfo.ArgumentList.Add(tempFilePath);

                using var process = Process.Start(startInfo);
                if (process == null)
                    return GeneralResult<string>.Failure("Failed to start Node.js helper process.");

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cts.Token);

                if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                    return GeneralResult<string>.Failure($"Node.js helper error: {error}");

                using var jsonDoc = JsonDocument.Parse(output);
                var statusCode = jsonDoc.RootElement.GetProperty("statusCode").GetInt32();
                var bodyString = jsonDoc.RootElement.GetProperty("body").GetString() ?? string.Empty;

                if (statusCode >= 200 && statusCode < 300)
                {
                    // Try to extract a "reply"/"response"/"message" field if JSON, otherwise use raw body
                    try
                    {
                        using var replyDoc = JsonDocument.Parse(bodyString);
                        if (replyDoc.RootElement.TryGetProperty("reply", out var replyProp))
                            return GeneralResult<string>.Success(replyProp.GetString() ?? bodyString, "Chatbot responded successfully.");
                        if (replyDoc.RootElement.TryGetProperty("answer", out var answerProp))
                            return GeneralResult<string>.Success(answerProp.GetString() ?? bodyString, "Chatbot responded successfully.");
                        if (replyDoc.RootElement.TryGetProperty("response", out var responseProp))
                            return GeneralResult<string>.Success(responseProp.GetString() ?? bodyString, "Chatbot responded successfully.");
                        if (replyDoc.RootElement.TryGetProperty("message", out var messageProp))
                            return GeneralResult<string>.Success(messageProp.GetString() ?? bodyString, "Chatbot responded successfully.");
                    }
                    catch { /* Body is plain text */ }

                    return GeneralResult<string>.Success(bodyString, "Chatbot responded successfully.");
                }

                return GeneralResult<string>.Failure($"Chatbot flow returned status {statusCode}: {bodyString}");
            }
            catch (OperationCanceledException)
            {
                return GeneralResult<string>.Failure("Chatbot flow timed out.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LangFlowService] Chatbot Exception: {ex.Message}");
                return GeneralResult<string>.Failure($"Exception triggering chatbot flow: {ex.Message}");
            }
            finally
            {
                try { if (File.Exists(tempFilePath)) File.Delete(tempFilePath); } catch { }
            }
        }
    }
}