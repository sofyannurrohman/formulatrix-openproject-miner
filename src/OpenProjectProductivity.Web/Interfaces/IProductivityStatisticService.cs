using OpenProductivity.Web.DTOs;

namespace OpenProductivity.Web.Interfaces
{
    public interface IProductivityStatisticService
    {
        Task<List<MemberStatisticDto>> GetProjectStatisticsAsync(
            int projectId,
            DateTime startPeriod,
            DateTime endPeriod,
            CancellationToken cancellationToken = default);

        Task<MemberTaskDetailsResponseDto> GetMemberTaskDetailsAsync(
            int projectId,
            int memberId,
            DateTime startPeriod,
            DateTime endPeriod,
            CancellationToken cancellationToken = default);
    }
}
