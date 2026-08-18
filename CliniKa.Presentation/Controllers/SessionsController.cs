using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.Constants;
using CliniKa.Application.DTOs.Langflow;
using CliniKa.Application.DTOs.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniKa.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly ILangFlowService _langFlowService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IDiagnosisNotificationService _notificationService;

        public SessionsController(
            ISessionService sessionService,
            ILangFlowService langFlowService,
            IWebHostEnvironment webHostEnvironment,
            IDiagnosisNotificationService notificationService)
        {
            _sessionService = sessionService;
            _langFlowService = langFlowService;
            _webHostEnvironment = webHostEnvironment;
            _notificationService = notificationService;
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
            {
                // Notify any waiting SSE clients that the diagnosis is complete
                _notificationService.NotifyDiagnosisComplete(sessionId);
                System.Console.WriteLine($"[DiagnosisCallback] SSE notification sent for Session {sessionId}");
                return Ok(saveResult);
            }

            if (saveResult.Status == ResultStatus.NotFound)
                return NotFound(saveResult);

            return BadRequest(saveResult);
        }

        /// <summary>
        /// Prescribes one or more medications for a patient during a clinical session.
        /// The prescribed medications are saved to the PrescribedMedications table
        /// and will be included in the drug interaction check payload.
        /// </summary>
        [HttpPost("{sessionId:int}/prescribe")]
        [Authorize(Roles = RoleConstants.Doctor)]
        public async Task<IActionResult> PrescribeMedications(
            [FromRoute] int sessionId,
            [FromBody] List<PrescribeMedicationRequest> medications)
        {
            if (medications == null || medications.Count == 0)
            {
                return BadRequest(GeneralResult.Failure("At least one medication is required."));
            }

            var result = await _sessionService.PrescribeMedicationsAsync(sessionId, medications);

            if (result.IsSuccess)
                return Ok(result);

            if (result.Status == ResultStatus.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        [HttpPost("{sessionId}/trigger-interaction-check")]
        public async Task<IActionResult> TriggerInteractionCheck(int sessionId)
        {
            var payloadResult = await _sessionService.GetWorkflowPayloadAsync(sessionId);

            if (!payloadResult.IsSuccess || payloadResult.Data == null)
                return BadRequest(payloadResult.Message);

            var workflowResult = await _langFlowService.TriggerDrugInteractionFlowAsync(payloadResult.Data);

            if (workflowResult.IsSuccess)
                return Ok(workflowResult);

            return StatusCode(500, workflowResult.Message);
        }

        /// <summary>
        /// Retrieves the diagnosis result stored for a specific clinical session.
        /// Reads the already processed AI result from the database without triggering the AI workflow again.
        /// </summary>
        [HttpGet("{sessionId:int}/diagnosis")]
        public async Task<IActionResult> GetDiagnosis([FromRoute] int sessionId)
        {
            var result = await _sessionService.GetSessionDiagnosisAsync(sessionId);

            if (result.IsSuccess)
                return Ok(result);

            if (result.Status == ResultStatus.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        /// <summary>
        /// SSE endpoint — the frontend connects here after triggering a diagnosis.
        /// The connection stays open until the callback arrives, then sends
        /// an "diagnosis-complete" event and closes.
        /// </summary>
        [HttpGet("{sessionId:int}/diagnosis-stream")]
        public async Task DiagnosisStream([FromRoute] int sessionId)
        {
            // 1. Set SSE headers
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            // 2. Send initial "connected" event so the frontend knows the stream is alive
            await Response.WriteAsync($"event: connected\ndata: {{\"sessionId\": {sessionId}}}\n\n");
            await Response.Body.FlushAsync();

            // 3. Wait for the diagnosis callback to arrive (or client to disconnect)
            var result = await _notificationService.WaitForNotificationAsync(
                sessionId, HttpContext.RequestAborted);

            if (result.IsSuccess)
            {
                // 4. Callback arrived — send the "diagnosis-complete" event
                await Response.WriteAsync($"event: diagnosis-complete\ndata: {{\"sessionId\": {sessionId}, \"status\": \"completed\"}}\n\n");
                await Response.Body.FlushAsync();
            }

            // 5. Connection closes automatically when the method returns.
            //    The frontend's EventSource will fire the 'error' event with
            //    readyState = CLOSED, which is the expected behavior.
        }

        [HttpGet("Hello")]
        public async Task<IActionResult> Click_Me()
        {
            return Ok("I am Clicked!");
        }
    }
}


