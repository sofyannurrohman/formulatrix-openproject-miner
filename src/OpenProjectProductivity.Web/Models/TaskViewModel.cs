namespace OpenProjectProductivity.Web.Models;
public class TaskViewModel
{
    public string Name { get; set; }
    public string Owner { get; set; }
    public string Status { get; set; } // "Completed", "Pending", "Overdue"
    public DateTime DueDate { get; set; }
}
