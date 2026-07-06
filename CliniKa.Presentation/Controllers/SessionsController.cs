using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CliniKa.Application;
using CliniKa.Application.DTOs;
using CliniKa.Application.Abstractions.Services;

namespace CliniKa.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionsController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpPost("{id:int}/transcribe")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Transcribe([FromRoute] int id, IFormFile audio)
        {
            if (audio == null || audio.Length == 0)
            {
                return BadRequest(GeneralResult.Failure("Audio file is required and cannot be empty."));
            }

            using var stream = audio.OpenReadStream();
            var result = await _sessionService.TranscribeSessionAudioAsync(id, stream, audio.FileName);

            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result);
            }

            return BadRequest(result);
        }
    }
}
