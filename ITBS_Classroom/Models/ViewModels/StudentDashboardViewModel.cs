namespace ITBS_Classroom.Models.ViewModels;

public class StudentDashboardViewModel
{
    public IReadOnlyList<Course> Courses { get; set; } = [];
    public int PendingAssignments { get; set; }
    public int CompletedSubmissions { get; set; }
    public IReadOnlyList<Assignment> UpcomingDeadlines { get; set; } = [];
}
