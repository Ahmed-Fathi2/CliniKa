using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;

namespace CliniKa.Infrastructure.Services
{
    public class LangFlowService : ILangFlowService
    {
        private const string WebhookUrl = "https://cloud.activepieces.com/api/v1/webhooks/aN5md3tHu7izwg01SvZ3C";
        private const string HelperScriptName = "langflow_helper.js";

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
                var scriptPath = ResolveHelperScriptPath();
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

        private static string? ResolveHelperScriptPath()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, HelperScriptName),
                Path.Combine(Directory.GetCurrentDirectory(), HelperScriptName),
                Path.Combine(Directory.GetCurrentDirectory(), "CliniKa.Presentation", HelperScriptName)
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path)) return path;
            }

            return null;
        }
    }
}
