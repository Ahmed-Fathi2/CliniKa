using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CliniKa.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IUserService _userService;

        public DashboardController(IDashboardService dashboardService, IUserService userService)
        {
            _dashboardService = dashboardService;
            _userService = userService;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<GeneralResult<DashboardSummaryDto>>> GetSummary()
        {
            var summaryData = await _dashboardService.GetDashboardDataAsync();

            var usersCount = await _userService.GetAllUsersAsync();

            summaryData.TotalUsers = usersCount.Data.Count();

            var result = GeneralResult<DashboardSummaryDto>.Success(summaryData);

            return Ok(result);
        }
    }
}
