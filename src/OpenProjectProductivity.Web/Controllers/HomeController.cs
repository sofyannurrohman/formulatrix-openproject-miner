using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenProjectProductivity.Web.Models;

namespace OpenProjectProductivity.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Example: Replace this with real OpenProject API data
        var tasks = new List<TaskViewModel>
        {
            new TaskViewModel { Name = "Design homepage", Owner = "Alice", Status = "Completed", DueDate = DateTime.Parse("2025-10-10") },
            new TaskViewModel { Name = "API integration", Owner = "Bob", Status = "Pending", DueDate = DateTime.Parse("2025-10-12") },
            new TaskViewModel { Name = "Write tests", Owner = "Charlie", Status = "Overdue", DueDate = DateTime.Parse("2025-10-08") }
        };

        // Calculate metrics
        var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(t => t.Status == "Completed");
        var pendingTasks = tasks.Count(t => t.Status == "Pending");
        var productivityScore = totalTasks == 0 ? 0 : (int)((double)completedTasks / totalTasks * 100);

        ViewData["TotalTasks"] = totalTasks;
        ViewData["CompletedTasks"] = completedTasks;
        ViewData["PendingTasks"] = pendingTasks;
        ViewData["ProductivityScore"] = productivityScore;

        return View(tasks);
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
