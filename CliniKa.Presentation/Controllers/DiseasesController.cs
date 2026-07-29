using System.Threading.Tasks;
using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Disease;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniKa.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiseasesController : ControllerBase
    {
        private readonly IDiseaseService _diseaseService;

        public DiseasesController(IDiseaseService diseaseService)
        {
            _diseaseService = diseaseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDiseases([FromQuery] string? search)
        {
            var result = await _diseaseService.GetAllDiseasesAsync(search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDiseaseById([FromRoute] int id)
        {
            var result = await _diseaseService.GetDiseaseByIdAsync(id);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateDisease([FromBody] CreateDiseaseRequest request)
        {
            var result = await _diseaseService.CreateDiseaseAsync(request);
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetDiseaseById), new { id = result.Data?.Id }, result);
            }
            if (result.Status == ResultStatus.ValidationError)
            {
                return BadRequest(result);
            }
            return BadRequest(result);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateDisease([FromRoute] int id, [FromBody] UpdateDiseaseRequest request)
        {
            var result = await _diseaseService.UpdateDiseaseAsync(id, request);
            if (result.IsSuccess)
            {
                return NoContent();
            }
            if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result);
            }
            if (result.Status == ResultStatus.ValidationError)
            {
                return BadRequest(result);
            }
            return BadRequest(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteDisease([FromRoute] int id)
        {
            var result = await _diseaseService.DeleteDiseaseAsync(id);
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }
    }
}
