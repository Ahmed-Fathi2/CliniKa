using CliniKa.Application;
using CliniKa.Application.Abstraction;
using CliniKa.Application.DTOs.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniKa.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchPatients([FromQuery] string query)
        {
            var result = await _patientService.GetPatientAsync(query);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        [HttpGet("history/{id:int}")]
        public async Task<IActionResult> GetPatientHistory([FromRoute] int id)
        {
            var result = await _patientService.GetPatientHistoryAsync(id);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        [HttpPost("medical-info")]
        [Authorize]
        public async Task<IActionResult> AddMedicalInfo([FromBody] AddMedicalInfoRequest request)
        {
            var result = await _patientService.AddMedicalInfoAsync(request);

            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result);
            }
            else if (result.Status == ResultStatus.ValidationError)
            {
                return BadRequest(result);
            }

            return BadRequest(result);
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetPatientProfile()
        {
            var result = await _patientService.GetPatientProfileAsync();
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        [HttpGet("medical-data")]
        [Authorize]
        public async Task<IActionResult> GetPatientMedicalData()
        {
            var result = await _patientService.GetPatientMedicalDataAsync();
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        [HttpGet("sessions")]
        [Authorize]
        public async Task<IActionResult> GetPatientSessions()
        {
            var result = await _patientService.GetPatientSessionsAsync();
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        [HttpGet("sessions/{sessionId:int}")]
        [Authorize]
        public async Task<IActionResult> GetPatientSessionDetails([FromRoute] int sessionId)
        {
            var result = await _patientService.GetPatientSessionDetailsAsync(sessionId);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }
    }
}
