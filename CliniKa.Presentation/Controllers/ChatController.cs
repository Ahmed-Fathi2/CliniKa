using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Chat;
using Microsoft.AspNetCore.Mvc;

namespace CliniKa.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Creates a new chat session for a patient.
        /// The backend fetches all patient data (medications, diseases, allergies)
        /// and builds the system prompt — Angular only sends the patientId.
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartChat([FromBody] StartChatRequestDto dto)
        {
            var result = await _chatService.StartChatAsync(dto.PatientId);

            if (result.IsSuccess)
                return Ok(result);

            if (result.Status == ResultStatus.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Sends a user message and returns the AI response.
        /// The backend manages the full conversation history — Angular only sends the message text.
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                return BadRequest(GeneralResult.Failure("Message cannot be empty."));
            }

            var result = await _chatService.SendMessageAsync(dto.ChatSessionId, dto.Message);

            if (result.IsSuccess)
                return Ok(result);

            if (result.Status == ResultStatus.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Returns the full message history for a chat session.
        /// Called when the patient navigates to a previous conversation from the sidebar.
        /// </summary>
        [HttpGet("{id:int}/history")]
        public async Task<IActionResult> GetChatHistory([FromRoute] int id)
        {
            var result = await _chatService.GetChatHistoryAsync(id);

            if (result.IsSuccess)
                return Ok(result);

            if (result.Status == ResultStatus.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Returns a list of all chat sessions for a patient (for sidebar display).
        /// Ordered by most recent message first.
        /// </summary>
        [HttpGet("patient/{patientId:int}")]
        public async Task<IActionResult> GetPatientChats([FromRoute] int patientId)
        {
            var result = await _chatService.GetPatientChatsAsync(patientId);

            if (result.IsSuccess)
                return Ok(result);

            if (result.Status == ResultStatus.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }
    }
}
