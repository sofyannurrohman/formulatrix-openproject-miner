namespace OpenProductivity.Web.Models;
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<WorkPackage> AssignedWorkPackages { get; set; } = new List<WorkPackage>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}