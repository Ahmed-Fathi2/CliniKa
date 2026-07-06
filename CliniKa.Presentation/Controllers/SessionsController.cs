using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CliniKa.Application;
using CliniKa.Application.Abstraction;
using CliniKa.Application.DTOs;

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
        public async Task<IActionResult> Transcribe([FromRoute] int id, [FromForm] IFormFile audio)
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
