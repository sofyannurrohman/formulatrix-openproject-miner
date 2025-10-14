// OpenProductivity.Web.ViewModels/MemberStatisticVm.cs
namespace OpenProductivity.Web.ViewModels
{
    public class MemberStatisticVm
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }

        // Same name as DTO
        public double AvgDurationDays { get; set; }
        public int ReworkCount { get; set; }
        public double ProductivityScore { get; set; }
    }
}
