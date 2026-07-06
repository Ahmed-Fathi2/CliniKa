using CliniKa.Application;
using CliniKa.Application.Abstraction;
using Microsoft.AspNetCore.Http;
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
        [HttpGet("{id:int}/history")]
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
    }
}
