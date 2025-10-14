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
            [FromQuery] string goalPeriod,
            CancellationToken cancellationToken)
        {
           
            var stats = await _statService.GetProjectStatisticsAsync(
                projectId, goalPeriod, cancellationToken);

            return Ok(stats);
        }

        
        [HttpGet("member/{memberId}/details")]
        public async Task<IActionResult> GetMemberTaskDetails(
            int projectId,
            int memberId,
            [FromQuery] string goalPeriod,
            CancellationToken token)
        {
            if (projectId <= 0 || memberId <= 0)
                return BadRequest("Invalid project or member ID.");

            var result = await _statService.GetMemberTaskDetailsAsync(
                projectId, memberId, goalPeriod, token);

            if (result == null || result.Tasks == null || !result.Tasks.Any())
                return NotFound("No tasks found for this member in the specified period.");

            return Ok(result);
        }

    }
}
