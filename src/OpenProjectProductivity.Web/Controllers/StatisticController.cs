using Microsoft.AspNetCore.Mvc;
using OpenProductivity.Web.Interfaces;

namespace OpenProductivity.Web.Controllers
{
    [ApiController]
    [Route("api/statistics/project/{projectId}")]
    public class StatisticsController : ControllerBase
    {
        private readonly IProductivityStatisticService _statService;

        public StatisticsController(IProductivityStatisticService statService)
        {
            _statService = statService ?? throw new ArgumentNullException(nameof(statService));
        }

        // GET api/statistics/project/{projectId}?startPeriod=yyyy-MM-dd&endPeriod=yyyy-MM-dd
        [HttpGet]
        public async Task<IActionResult> GetProjectStatistics(
            int projectId,
            [FromQuery] DateTime startPeriod,
            [FromQuery] DateTime endPeriod,
            CancellationToken cancellationToken)
        {
            if (startPeriod >= endPeriod)
                return BadRequest("Invalid date range");

            var stats = await _statService.GetProjectStatisticsAsync(
                projectId, startPeriod, endPeriod, cancellationToken);

            return Ok(stats);
        }

        // GET api/statistics/project/{projectId}/member/{memberId}/details?startPeriod=yyyy-MM-dd&endPeriod=yyyy-MM-dd
        [HttpGet("member/{memberId}/details")]
        public async Task<IActionResult> GetMemberTaskDetails(
            int projectId,
            int memberId,
            [FromQuery] DateTime startPeriod,
            [FromQuery] DateTime endPeriod,
            CancellationToken token)
        {
            if (projectId <= 0 || memberId <= 0)
                return BadRequest("Invalid project or member ID.");

            if (startPeriod >= endPeriod)
                return BadRequest("Invalid date range.");

            var result = await _statService.GetMemberTaskDetailsAsync(
                projectId, memberId, startPeriod, endPeriod, token);

            if (result == null || result.Tasks == null || !result.Tasks.Any())
                return NotFound("No tasks found for this member in the specified period.");

            return Ok(result);
        }

    }
}
