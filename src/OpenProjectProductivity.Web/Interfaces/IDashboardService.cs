using OpenProductivity.Web.DTOs;
using System.Threading.Tasks;

namespace OpenProjectProductivity.Web.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardCountsDto> GetDashboardCountsAsync();
    }
}
