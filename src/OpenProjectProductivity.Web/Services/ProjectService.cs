using Microsoft.EntityFrameworkCore;
using OpenProductivity.Web.Data;
using OpenProductivity.Web.DTOs;
using OpenProductivity.Web.Interfaces;

public class ProjectService : IProjectService
{
    private readonly OpenProjectContext _context;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(OpenProjectContext context, ILogger<ProjectService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProjectDto>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _context.Projects
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Fetched {Count} projects", projects.Count);
        return projects;
    }

    public async Task<ProjectDto?> GetProjectByIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects
            .Where(p => p.Id == projectId)
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (project == null)
            _logger.LogWarning("Project {ProjectId} not found", projectId);
        else
            _logger.LogInformation("Fetched project {ProjectId}: {ProjectName}", project.Id, project.Name);

        return project;
    }
    
}
