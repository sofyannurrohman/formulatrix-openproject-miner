using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenProductivity.Web.DTOs;
using OpenProductivity.Web.Interfaces;
using System.Threading;

namespace OpenProductivity.Web.Controllers
{
    // Use cookie authentication for MVC pages
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IProductivityStatisticService _statService;
        private readonly IProjectService _projectService;

        public HomeController(
            IProductivityStatisticService statService,
            IProjectService projectService)
        {
            _statService = statService;
            _projectService = projectService;
        }

        // Dashboard
        public async Task<IActionResult> Index(int? projectId, string goalPeriod, CancellationToken cancellationToken = default)
        {
            ViewBag.ProjectId = projectId;
            ViewBag.GoalPeriod = goalPeriod;

            // Fetch list of projects dynamically
            var projects = await _projectService.GetAllProjectsAsync(cancellationToken);
            ViewBag.Projects = projects.Select(p => (p.Id, p.Name)).ToList();

            // Example goal periods
            ViewBag.GoalPeriods = new List<string> { "2025-H1", "2025-H2", "2024-H1", "2024-H2", "2023-H1", "2023-H2", "2022-H1", "2022-H2", "2021-H1", "2021-H2" };

            // Fetch member statistics if project and goal period are selected
            List<MemberStatisticDto> statistics = new();
            if (projectId.HasValue && !string.IsNullOrEmpty(goalPeriod))
            {
                statistics = await _statService.GetProjectStatisticsAsync(projectId.Value, goalPeriod, cancellationToken);
            }

            return View(statistics);
        }

        public async Task<IActionResult> MemberTasks(int projectId, int memberId, string goalPeriod, CancellationToken cancellationToken = default)
        {
            if (projectId <= 0 || memberId <= 0 || string.IsNullOrEmpty(goalPeriod))
                return BadRequest("Invalid parameters.");

            // Fetch member task details
            var memberTasks = await _statService.GetMemberTaskDetailsAsync(projectId, memberId, goalPeriod, cancellationToken);

            if (memberTasks == null || memberTasks.Tasks == null || !memberTasks.Tasks.Any())
                return View("MemberTasks", null); // or show a message in the view

            return View(memberTasks);
        }
    }
}
