using ITBS_Classroom.Domain.Entities;

namespace ITBS_Classroom.Models.Dashboard;

public class TeacherDashboardViewModel
{
    public IReadOnlyList<ClassGroup> Groups { get; set; } = [];
    public IReadOnlyList<Course> Courses { get; set; } = [];
}
