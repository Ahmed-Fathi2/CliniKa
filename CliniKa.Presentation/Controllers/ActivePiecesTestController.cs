using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CliniKa.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivePiecesTestController : ControllerBase
    {
        //https://cloud.activepieces.com/api/v1/webhooks/Jg8uqP6OMXCpUsatpxUzt/sync
        [HttpPost("test")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> TestActivepieces(
            IFormFile audio,
            IFormFile lab1,
            [FromForm] string sessionId)
        {
            Console.WriteLine($"\n=== [ActivePiecesTest via Node.js Proxy] Request Received ===");
            Console.WriteLine($"Session ID: {sessionId}");
            Console.WriteLine($"Audio File: Name={audio?.FileName}, Size={audio?.Length} bytes, ContentType={audio?.ContentType}");
            Console.WriteLine($"Lab1 File: Name={lab1?.FileName}, Size={lab1?.Length} bytes, ContentType={lab1?.ContentType}");
            Console.WriteLine("============================================================\n");

            if (audio == null || audio.Length == 0)
            {
                return BadRequest(new { Error = "Audio file is required." });
            }

            if (lab1 == null || lab1.Length == 0)
            {
                return BadRequest(new { Error = "Lab1 image file is required." });
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { Error = "SessionId is required." });
            }

            // Create temp file paths to hand over to Node.js
            var tempAudioPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(audio.FileName));
            var tempLabPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(lab1.FileName));

            try
            {
                // Save files locally to temp folder
                using (var stream = new FileStream(tempAudioPath, FileMode.Create))
                {
                    await audio.CopyToAsync(stream);
                }

                using (var stream = new FileStream(tempLabPath, FileMode.Create))
                {
                    await lab1.CopyToAsync(stream);
                }

                // Resolve path to activepieces_helper.js
                var scriptPath = "activepieces_helper.js";
                var searchPaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "activepieces_helper.js"),
                    Path.Combine(Directory.GetCurrentDirectory(), "activepieces_helper.js"),
                    Path.Combine(Directory.GetCurrentDirectory(), "CliniKa.Presentation", "activepieces_helper.js")
                };

                foreach (var path in searchPaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        scriptPath = path;
                        break;
                    }
                }

                var url = "https://cloud.activepieces.com/api/v1/webhooks/Jg8uqP6OMXCpUsatpxUzt/sync";

                // Execute Node.js script to run the fetch with OpenSSL TLS stack (bypassing SChannel TLS bugs)
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = $"\"{scriptPath}\" \"{url}\" \"{sessionId}\" \"{tempAudioPath}\" \"{audio.FileName}\" \"{tempLabPath}\" \"{lab1.FileName}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Console.WriteLine($"[ActivePiecesTest] Starting Node.js helper with script: {scriptPath}");
                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Failed to start Node.js helper process." });
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Node.js helper process exited with error", Details = error });
                }

                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(output);
                    var root = doc.RootElement;
                    var statusCode = root.GetProperty("statusCode").GetInt32();
                    var bodyString = root.GetProperty("body").GetString() ?? string.Empty;

                    if (statusCode >= 200 && statusCode < 300)
                    {
                        try
                        {
                            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(bodyString);
                            return StatusCode(statusCode, jsonNode);
                        }
                        catch
                        {
                            return Content(bodyString, "application/json");
                        }
                    }
                    else
                    {
                        return StatusCode(statusCode, new { Error = "External API Error", Details = bodyString });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Failed to parse helper output", Output = output, Exception = ex.Message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ActivePiecesTest] Exception occurred: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "An exception occurred while processing request", Message = ex.Message });
            }
            finally
            {
                // Clean up temp files
                try
                {
                    if (System.IO.File.Exists(tempAudioPath))
                    {
                        System.IO.File.Delete(tempAudioPath);
                    }
                }
                catch { }

                try
                {
                    if (System.IO.File.Exists(tempLabPath))
                    {
                        System.IO.File.Delete(tempLabPath);
                    }
                }
                catch { }
            }
        }
    }
}
