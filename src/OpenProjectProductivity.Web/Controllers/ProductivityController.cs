using Microsoft.AspNetCore.Mvc;
using OpenProductivity.Web.ViewModels;
using System.Net.Http.Json;

namespace OpenProductivity.Web.Controllers
{
    public class ProductivityController : Controller
    {
        private readonly HttpClient _http;

        public ProductivityController(IHttpClientFactory clientFactory)
        {
            _http = clientFactory.CreateClient("OpenProjectApi");
        }

        public async Task<IActionResult> Index(int projectId, DateTime? startPeriod, DateTime? endPeriod, CancellationToken token)
        {
            if (projectId <= 0)
                return BadRequest("Invalid project ID.");

            var start = startPeriod ?? DateTime.UtcNow.AddMonths(-1);
            var end = endPeriod ?? DateTime.UtcNow;

            // ✅ Call your API
            var url = $"api/statistics/project/{projectId}?startPeriod={start:yyyy-MM-dd}&endPeriod={end:yyyy-MM-dd}";
            var stats = await _http.GetFromJsonAsync<List<MemberStatisticVm>>(url, token);

            if (stats == null || !stats.Any())
                ViewBag.Message = "No productivity statistics available for this project.";

            return View(stats);
        }
    }
}
