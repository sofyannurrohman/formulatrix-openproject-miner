using OpenProductivity.Web.DTOs;

namespace OpenProductivity.Web.Interfaces
{
    public interface IProductivityStatisticService
    {
        Task<List<MemberStatisticDto>> GetProjectStatisticsAsync(
            int projectId,
            string goalPeriod,
            CancellationToken cancellationToken = default);

        Task<MemberTaskDetailsResponseDto> GetMemberTaskDetailsAsync(
            int projectId,
            int memberId,
            string goalPeriod,
            CancellationToken cancellationToken = default);
    }
}
