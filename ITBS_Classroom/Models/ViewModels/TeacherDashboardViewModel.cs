namespace ITBS_Classroom.Models.ViewModels;

public class TeacherDashboardViewModel
{
    public IReadOnlyList<Course> Courses { get; set; } = [];
    public int TotalStudents { get; set; }
    public int PendingSubmissions { get; set; }
    public int UpcomingDeadlines { get; set; }
}
