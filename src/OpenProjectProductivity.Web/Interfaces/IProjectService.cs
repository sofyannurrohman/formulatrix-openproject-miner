using OpenProductivity.Web.DTOs;

namespace OpenProductivity.Web.Interfaces;

public interface IProjectService
{
    Task<List<ProjectDto>> GetAllProjectsAsync(CancellationToken cancellationToken = default);
    Task<ProjectDto?> GetProjectByIdAsync(int projectId, CancellationToken cancellationToken = default);
}
