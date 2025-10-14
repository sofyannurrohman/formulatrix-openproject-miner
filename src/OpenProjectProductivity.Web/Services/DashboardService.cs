using Microsoft.EntityFrameworkCore;
using OpenProductivity.Web.Data;
using OpenProductivity.Web.DTOs;
using OpenProjectProductivity.Web.Interfaces;

namespace OpenProjectProductivity.Web.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly OpenProjectContext _context;

        public DashboardService(OpenProjectContext context)
        {
            _context = context;
        }

        public async Task<DashboardCountsDto> GetDashboardCountsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalProjects = await _context.Projects.CountAsync();
            var totalWorkPackages = await _context.WorkPackages.CountAsync();
            var totalActivities = await _context.Activities.CountAsync();

            return new DashboardCountsDto
            {
                TotalUsers = totalUsers,
                TotalProjects = totalProjects,
                TotalWorkPackages = totalWorkPackages,
                TotalActivities = totalActivities
            };
        }
    }
}
