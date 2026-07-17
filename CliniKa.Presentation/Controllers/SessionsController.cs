using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Sessions;
using CliniKa.Application.DTOs.Langflow;

namespace CliniKa.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SessionsController(
            ISessionService sessionService,
            IWebHostEnvironment webHostEnvironment)
        {
            _sessionService = sessionService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateClinicalSessionDto dto)
        {
            var result = await _sessionService.CreateSessionAsync(dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:int}/diagnose")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Diagnose(
            [FromRoute] int id,
            IFormFile audio,
            List<IFormFile>? documents)
        {
            System.Console.WriteLine($"\n=== [Diagnose Endpoint] Request Received ===");
            System.Console.WriteLine($"Session ID: {id}");
            System.Console.WriteLine($"Audio File: Name={audio?.FileName}, Size={audio?.Length} bytes");
            if (documents != null)
            {
                System.Console.WriteLine($"Documents Count: {documents.Count}");
                foreach (var doc in documents)
                {
                    System.Console.WriteLine($" - Document: Name={doc.FileName}, Size={doc.Length} bytes");
                }
            }
            System.Console.WriteLine("===========================================\n");

            if (audio == null || audio.Length == 0)
            {
                return BadRequest(GeneralResult.Failure("Audio file is required and cannot be empty."));
            }

            var schema = Request.Scheme;
            var host = Request.Host.Value;
            var baseUrl = _webHostEnvironment.WebRootPath;

            var result = await _sessionService.DiagnoseSessionAsync(id, audio, documents, schema, host, baseUrl);

            if (result.IsSuccess)
                return Ok(result);

            if (result.Status == ResultStatus.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        [HttpPost("/api/{id}/callback")]
        [HttpPost("{id}/callback")]
        public async Task<IActionResult> DiagnosisCallback(
            [FromRoute] string id,
            [FromBody] LangFlowDiagnosisResult result)
        {
            if (!int.TryParse(id, out var sessionId))
            {
                System.Console.WriteLine($"[DiagnosisCallback] Error: Invalid Session ID format: '{id}'");
                return BadRequest(GeneralResult.Failure($"Invalid Session ID format: '{id}'"));
            }

            // Enable buffering to read the raw request body
            Request.EnableBuffering();
            Request.Body.Position = 0;
            using (var reader = new System.IO.StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                var rawBody = await reader.ReadToEndAsync();
                System.Console.WriteLine($"\n=== [DiagnosisCallback] RAW REQUEST BODY RECEIVED FOR SESSION {sessionId} ===");
                System.Console.WriteLine(rawBody);
                System.Console.WriteLine("=====================================================\n");
            }
            Request.Body.Position = 0; // Reset position for model binder

            System.Console.WriteLine($"\n=== [DiagnosisCallback] Parsed Callback for SessionId: {sessionId} ===");
            if (result == null)
            {
                System.Console.WriteLine("[DiagnosisCallback] Error: Result payload is null.");
                return BadRequest(GeneralResult.Failure("Callback payload is empty."));
            }

            System.Console.WriteLine($"[DiagnosisCallback] AudioTranscript: {result.Body?.Transcript}");
            System.Console.WriteLine($"[DiagnosisCallback] Chief Complaint: {result.Body?.ChiefComplaint}");
            System.Console.WriteLine($"[DiagnosisCallback] Possible Diagnosis: {result.Body?.PossibleDiagnosis}");
            System.Console.WriteLine("===================================================\n");

            var saveResult = await _sessionService.SaveDiagnosisCallbackResultAsync(sessionId, result);

            if (saveResult.IsSuccess)
                return Ok(saveResult);

            if (saveResult.Status == ResultStatus.NotFound)
                return NotFound(saveResult);

            return BadRequest(saveResult);
        }
    }
}


