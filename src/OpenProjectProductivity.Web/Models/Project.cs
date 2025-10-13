namespace OpenProductivity.Web.Models;
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<WorkPackage> WorkPackages { get; set; } = new List<WorkPackage>();
}