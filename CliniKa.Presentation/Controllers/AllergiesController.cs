using System.Threading.Tasks;
using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Allergy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniKa.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AllergiesController : ControllerBase
    {
        private readonly IAllergyService _allergyService;

        public AllergiesController(IAllergyService allergyService)
        {
            _allergyService = allergyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAllergies([FromQuery] string? search)
        {
            var result = await _allergyService.GetAllAllergiesAsync(search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAllergyById([FromRoute] int id)
        {
            var result = await _allergyService.GetAllergyByIdAsync(id);
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
        public async Task<IActionResult> CreateAllergy([FromBody] CreateAllergyRequest request)
        {
            var result = await _allergyService.CreateAllergyAsync(request);
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetAllergyById), new { id = result.Data?.Id }, result);
            }
            if (result.Status == ResultStatus.ValidationError)
            {
                return BadRequest(result);
            }
            return BadRequest(result);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateAllergy([FromRoute] int id, [FromBody] UpdateAllergyRequest request)
        {
            var result = await _allergyService.UpdateAllergyAsync(id, request);
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
        public async Task<IActionResult> DeleteAllergy([FromRoute] int id)
        {
            var result = await _allergyService.DeleteAllergyAsync(id);
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
