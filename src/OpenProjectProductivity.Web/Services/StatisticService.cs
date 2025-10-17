using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenProductivity.Web.Data;
using OpenProductivity.Web.DTOs;
using OpenProductivity.Web.Interfaces;
using OpenProductivity.Web.Models;

namespace OpenProductivity.Web.Services
{
    public class ProductivityStatisticService : IProductivityStatisticService
    {
        private readonly OpenProjectContext _context;
        private readonly ILogger<ProductivityStatisticService> _logger;

        private static readonly string[] InProgressStatuses = { "In Progress", "In progress", "Developed" };
        private static readonly string[] DoneStatuses = { "Done", "Solved" };

        public ProductivityStatisticService(OpenProjectContext context, ILogger<ProductivityStatisticService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<MemberStatisticDto>> GetProjectStatisticsAsync(
            int projectId,
            string goalPeriod,
            CancellationToken cancellationToken = default)
        {
            var workPackages = await _context.WorkPackages
                .Where(wp => wp.ProjectId == projectId && wp.GoalPeriod == goalPeriod)
                .Include(wp => wp.Activities)
                .Include(wp => wp.Assignee)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} work packages for project {ProjectId} with GoalPeriod {GoalPeriod}", workPackages.Count, projectId, goalPeriod);

            var grouped = workPackages.GroupBy(wp => wp.AssigneeId);
            var statistics = new List<MemberStatisticDto>();

            foreach (var group in grouped)
            {
                var memberId = group.Key;
                var memberName = group.FirstOrDefault()?.Assignee?.Name ?? "Unassigned User";
                var memberTasks = group.ToList();

                int totalUserStories = memberTasks.Count(wp => string.Equals(wp.Type, "User story", StringComparison.OrdinalIgnoreCase));
                int totalIssues = memberTasks.Count(wp => string.Equals(wp.Type, "Issue", StringComparison.OrdinalIgnoreCase));
                int completedTasks = memberTasks.Count(wp => DoneStatuses.Contains(wp.Status ?? "", StringComparer.OrdinalIgnoreCase));

                var durations = memberTasks
                    .Where(wp => DoneStatuses.Contains(wp.Status ?? "", StringComparer.OrdinalIgnoreCase))
                    .Select(wp => CalculateInProgressToDoneDuration(wp))
                    .Where(d => d.HasValue)
                    .Select(d => d.Value)
                    .ToList();

                double avgDurationDays = durations.Any() ? durations.Average() : 0;
                int reworkTasks = memberTasks.Count(wp => DetectReworkLoop(wp.Activities));

                double baselineDuration = 10.0;
                double speedFactor = Math.Pow(baselineDuration / (avgDurationDays + 1), 0.5);

                double productivityScore = (completedTasks * 10 * speedFactor) - (reworkTasks * 5);
                productivityScore = Math.Clamp(productivityScore, 0, 100);

                statistics.Add(new MemberStatisticDto
                {
                    MemberId = memberId,
                    MemberName = memberName,
                    TotalUserStories = totalUserStories,
                    TotalIssues = totalIssues,
                    CompletedTasks = completedTasks,
                    AvgDurationDays = Math.Round(avgDurationDays, 2),
                    ReworkCount = reworkTasks,
                    ProductivityScore = productivityScore
                });
            }

            return statistics;
        }

        public async Task<MemberTaskDetailsResponseDto> GetMemberTaskDetailsAsync(
            int projectId,
            int memberId,
            string goalPeriod,
            CancellationToken cancellationToken = default)
        {
            var workPackages = await _context.WorkPackages
                .Where(wp => wp.ProjectId == projectId && wp.AssigneeId == memberId && wp.GoalPeriod == goalPeriod)
                .Include(wp => wp.Activities)
                .Include(wp => wp.Assignee)
                .ToListAsync(cancellationToken);

            var memberName = workPackages.FirstOrDefault()?.Assignee?.Name ?? "Unassigned User";
            var details = new List<MemberTaskDetailDto>();

            foreach (var wp in workPackages)
            {
                var activities = wp.Activities.OrderBy(a => a.Timestamp).ToList();
                var statusHistory = string.Join(" → ", activities.Select(a => a.ToStatus ?? "-"));

                // Start timestamp: first In Progress if exists, otherwise first activity
                var inProgressTimestamp = activities
                    .Where(a => InProgressStatuses.Contains(a.ToStatus ?? "", StringComparer.OrdinalIgnoreCase))
                    .Select(a => a.Timestamp)
                    .FirstOrDefault();

                var startTimestamp = inProgressTimestamp != default
                    ? inProgressTimestamp
                    : activities.Select(a => a.Timestamp).FirstOrDefault();

                var doneTimestamp = activities
                    .Where(a => DoneStatuses.Contains(a.ToStatus ?? "", StringComparer.OrdinalIgnoreCase))
                    .Select(a => a.Timestamp)
                    .LastOrDefault();

                double? duration = null;
                if (startTimestamp != default && doneTimestamp != default)
                    duration = (doneTimestamp - startTimestamp).TotalDays;

                bool reworkDetected = DetectReworkLoop(activities);

                details.Add(new MemberTaskDetailDto
                {
                    WorkPackageId = wp.Id,
                    Type = wp.Type,
                    Subject = wp.Subject,
                    Start = startTimestamp,
                    End = doneTimestamp,
                    DurationDays = duration.HasValue ? Math.Round(duration.Value, 2) : (double?)null,
                    StatusHistory = statusHistory,
                    Notes = reworkDetected ? $"Rework detected ({CountReworkLoops(activities)} loop(s))" : "No rework"
                });
            }

            return new MemberTaskDetailsResponseDto
            {
                MemberId = memberId,
                MemberName = memberName,
                Tasks = details
            };
        }

        private double? CalculateInProgressToDoneDuration(WorkPackage wp, string aggregationMode = "first")
        {
            var activities = wp.Activities.OrderBy(a => a.Timestamp).ToList();
            var inProgressTimestamps = activities
                .Where(a => InProgressStatuses.Contains(a.ToStatus ?? "", StringComparer.OrdinalIgnoreCase))
                .Select(a => a.Timestamp)
                .ToList();

            var doneTimestamps = activities
                .Where(a => DoneStatuses.Contains(a.ToStatus ?? "", StringComparer.OrdinalIgnoreCase))
                .Select(a => a.Timestamp)
                .ToList();

            if (!doneTimestamps.Any()) return null;
            if (!inProgressTimestamps.Any()) return (doneTimestamps.Min() - activities[0].Timestamp).TotalDays;
            // if (!inProgressTimestamps.Any()) return (doneTimestamps.Min() - wp.CreatedAt).TotalDays;

            var durations = new List<double>();
            foreach (var inProg in inProgressTimestamps)
            {
                var done = doneTimestamps.FirstOrDefault(d => d > inProg);
                if (done != default) durations.Add((done - inProg).TotalDays);
            }

            if (!durations.Any()) return null;

            return aggregationMode switch
            {
                "first" => durations.First(),
                "last" => durations.Last(),
                "average" => durations.Average(),
                _ => durations.First()
            };
        }

        private bool DetectReworkLoop(IEnumerable<Activity> activities)
        {
            return CountReworkLoops(activities.ToList()) > 0;
        }

        private int CountReworkLoops(List<Activity> activities)
        {
            int reworkCount = 0;
            string? prevStatus = null;
            bool seenInProgress = false;

            foreach (var a in activities.OrderBy(x => x.Timestamp))
            {
                var status = a.ToStatus ?? "";

                if (!string.IsNullOrEmpty(prevStatus))
                {
                    if ((string.Equals(prevStatus, "Developed", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(prevStatus, "Solved", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(prevStatus, "Done", StringComparison.OrdinalIgnoreCase)) &&
                        string.Equals(status, "In Progress", StringComparison.OrdinalIgnoreCase))
                    {
                        reworkCount++;
                    }
                }

                if (string.Equals(status, "In Progress", StringComparison.OrdinalIgnoreCase))
                {
                    if (seenInProgress) reworkCount++;
                    seenInProgress = true;
                }

                prevStatus = status;
            }

            return reworkCount;
        }

        public async Task<List<string>> GetAvailableGoalPeriodsAsync(int projectId, CancellationToken cancellationToken = default)
        {
            var periods = await _context.WorkPackages
                .Where(wp => wp.ProjectId == projectId)
                .Select(wp => wp.GoalPeriod)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync(cancellationToken);

            return periods;
        }
    }
}
