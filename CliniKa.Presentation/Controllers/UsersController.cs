using System.Threading.Tasks;
using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.Constants;
using CliniKa.Application.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CliniKa.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RoleConstants.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var result = await _userService.CreateUserAsync(request);
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetUserById), new { id = result.Data?.Id }, result);
            }
            return BadRequest(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById([FromRoute] string id)
        {
            var result = await _userService.GetUserByIdAsync(id);
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

        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateUser([FromRoute] string id)
        {
            var result = await _userService.ActivateUserAsync(id);
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

        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser([FromRoute] string id)
        {
            var result = await _userService.DeactivateUserAsync(id);
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

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> SetUserStatus([FromRoute] string id, [FromQuery] bool isActive)
        {
            var result = await _userService.SetUserActiveStatusAsync(id, isActive);
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
