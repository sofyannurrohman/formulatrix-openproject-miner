namespace OpenProductivity.Web.Models;

public class Activity
{
    public int Id { get; set; }
    public int WorkPackageId { get; set; }
    public WorkPackage WorkPackage { get; set; } = null!;

    public string? FromStatus { get; set; }    // previous status
    public string? ToStatus { get; set; }      // new status
    public string? Comment { get; set; }       // comment text, if any
    public DateTime Timestamp { get; set; }
    public int? UserId { get; set; }   
    public User? User { get; set; }
}
