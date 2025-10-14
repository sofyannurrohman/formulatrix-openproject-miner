namespace OpenProductivity.Web.DTOs;

public class MemberStatisticDto
{
    public int? MemberId { get; set; }
    public string MemberName { get; set; } = "";
    public int TotalUserStories { get; set; }
    public int TotalIssues { get; set; }
    public int CompletedTasks { get; set; }
    public double AvgDurationDays { get; set; }
    public int ReworkCount { get; set; }
    public double ProductivityScore { get; set; }
}
