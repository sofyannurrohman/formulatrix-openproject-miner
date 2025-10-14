using Microsoft.AspNetCore.Mvc;
using OpenProjectProductivity.Web.Interfaces;
using OpenProjectProductivity.Web.Services;
using System.Threading.Tasks;

namespace OpenProjectProductivity.Web.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var counts = await _dashboardService.GetDashboardCountsAsync();
            return View(counts); // pass DTO to Dashboard view
        }
    }
}
