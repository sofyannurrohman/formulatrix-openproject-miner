namespace OpenProductivity.Web.DTOs;

public class MemberTaskDetailsResponseDto
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public List<MemberTaskDetailDto> Tasks { get; set; } = new();
}

public class MemberTaskDetailDto
{
    public int WorkPackageId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public double? DurationDays { get; set; }
    public string StatusHistory { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
