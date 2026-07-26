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
        private const string WebhookUrl = "https://cloud.activepieces.com/api/v1/webhooks/aN5md3tHu7izwg01SvZ3C";
        private const string DrugInteractionWebhookUrl = "https://cloud.activepieces.com/api/v1/webhooks/irtY6rMKe4Mb1fVjwTuGv/sync";
        // TODO: Replace with your actual ActivePieces chatbot flow webhook URL
        private const string ChatbotWebhookUrl = "https://cloud.activepieces.com/api/v1/webhooks/YOUR_CHATBOT_FLOW_ID/sync";
        private const string HelperScriptName = "langflow_helper.js";
        private const string HelperInteractionsScriptName = "langflow_trigger_interaction_json_helper.js";

        public LangFlowService()
        {
        }

        public async Task<GeneralResult<string>> TriggerDiagnosisFlowAsync(
            int sessionId,
            IFormFile audioFile,
            List<IFormFile>? documentFiles)
        {
            var tempFiles = new List<string>();
            // Enforce 60 seconds cancellation boundary for process safety
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            try
            {
                // 1. Save audio to temp file
                var tempAudioPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(audioFile.FileName)}");
                await SaveFormFileToTempAsync(audioFile, tempAudioPath);
                tempFiles.Add(tempAudioPath);

                // 2. Save document files to temp
                var docTempPaths = new List<(string Path, string OriginalName)>();
                if (documentFiles != null)
                {
                    foreach (var doc in documentFiles)
                    {
                        if (doc.Length == 0) continue;
                        var tempDocPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(doc.FileName)}");
                        await SaveFormFileToTempAsync(doc, tempDocPath);
                        tempFiles.Add(tempDocPath);
                        docTempPaths.Add((tempDocPath, doc.FileName));
                    }
                }

                // 3. Resolve path cleanly
                var scriptPath = ResolveHelperScriptPath(HelperScriptName);
                if (scriptPath == null)
                {
                    return GeneralResult<string>.Failure($"Could not find {HelperScriptName}. Ensure it exists alongside the execution directory.");
                }

                // 4. Build process using ArgumentList (safe escaping for paths with spaces)
                var startInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                startInfo.ArgumentList.Add(scriptPath);
                startInfo.ArgumentList.Add(WebhookUrl);
                startInfo.ArgumentList.Add(sessionId.ToString());
                startInfo.ArgumentList.Add(tempAudioPath);
                startInfo.ArgumentList.Add(audioFile.FileName);

                foreach (var (path, name) in docTempPaths)
                {
                    startInfo.ArgumentList.Add(path);
                    startInfo.ArgumentList.Add(name);
                }

                Console.WriteLine($"[LangFlowService] Triggering Activepieces flow for Session {sessionId}...");
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return GeneralResult<string>.Failure("Failed to start Node.js helper process.");
                }

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // Structured waiting bound to the cancellation lifecycle token
                await process.WaitForExitAsync(cts.Token);

                var output = await outputTask;
                var error = await errorTask;

                Console.WriteLine($"[LangFlowService] Node.js helper exited with code {process.ExitCode}");

                if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                {
                    Console.WriteLine($"[LangFlowService] Error: {error}");
                    return GeneralResult<string>.Failure($"Node.js helper error: {error}");
                }

                // 5. Parse JSON output
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(output);
                var root = jsonDoc.RootElement;
                var statusCode = root.GetProperty("statusCode").GetInt32();
                var body = root.GetProperty("body").GetString() ?? string.Empty;

                Console.WriteLine($"[LangFlowService] Activepieces responded with status {statusCode}");

                if (statusCode >= 200 && statusCode < 300)
                {
                    return GeneralResult<string>.Success(body, "Activepieces flow triggered successfully.");
                }

                return GeneralResult<string>.Failure($"Activepieces returned status {statusCode}. Details: {body}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[LangFlowService] Execution aborted due to target timeout constraint.");
                return GeneralResult<string>.Failure("External workflow orchestration process timed out.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LangFlowService] Exception: {ex.Message}");
                return GeneralResult<string>.Failure($"Exception triggering Activepieces flow: {ex.Message}");
            }
            finally
            {
                // Clean up temp files safely
                foreach (var tempFile in tempFiles)
                {
                    try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
                }
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
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"payload_{Guid.NewGuid()}.json");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            try
            {
                Console.WriteLine($"[LangFlowService] Triggering Drug Interaction Flow for Session {payload.SessionId}...");

                var jsonPayload = JsonSerializer.Serialize(payload);

                await File.WriteAllTextAsync(tempFilePath, jsonPayload, cts.Token);

                var scriptPath = ResolveHelperScriptPath(HelperInteractionsScriptName);
                if (scriptPath == null)
                    return GeneralResult<object>.Failure($"Could not find {HelperInteractionsScriptName}.");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                startInfo.ArgumentList.Add(scriptPath);
                startInfo.ArgumentList.Add(DrugInteractionWebhookUrl);
                startInfo.ArgumentList.Add(tempFilePath);

                using var process = Process.Start(startInfo);
                if (process == null) return GeneralResult<object>.Failure("Failed to start Node process.");

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cts.Token);

                if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                    return GeneralResult<object>.Failure($"Node.js helper error: {error}");

                using var jsonDoc = JsonDocument.Parse(output);
                var statusCode = jsonDoc.RootElement.GetProperty("statusCode").GetInt32();
                var bodyString = jsonDoc.RootElement.GetProperty("body").GetString() ?? string.Empty;

                if (statusCode >= 200 && statusCode < 300)
                {
                    var parsedBody = JsonSerializer.Deserialize<object>(bodyString);
                    return GeneralResult<object>.Success(parsedBody, "Triggered successfully via Node.js");
                }

                return GeneralResult<object>.Failure($"Status {statusCode}: {bodyString}");
            }
            catch (Exception ex)
            {
                return GeneralResult<object>.Failure($"Exception: {ex.Message}");
            }
            finally
            {
                try { if (File.Exists(tempFilePath)) File.Delete(tempFilePath); } catch { }
            }
        }


        /// <summary>
        /// Sends system prompt + conversation history to the ActivePieces chatbot flow.
        /// Uses the same Node.js helper pattern (JSON payload via temp file) for SSL compatibility.
        /// </summary>
        public async Task<GeneralResult<string>> TriggerChatbotFlowAsync(ChatFlowPayloadDto payload)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"chat_payload_{Guid.NewGuid()}.json");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

            try
            {
                Console.WriteLine($"[LangFlowService] Triggering Chatbot Flow...");

                var jsonPayload = JsonSerializer.Serialize(payload);
                await File.WriteAllTextAsync(tempFilePath, jsonPayload, cts.Token);

                var scriptPath = ResolveHelperScriptPath(HelperInteractionsScriptName);
                if (scriptPath == null)
                    return GeneralResult<string>.Failure($"Could not find {HelperInteractionsScriptName}.");

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