namespace ITBS_Classroom.Models.ViewModels;

public class CourseDetailViewModel
{
    public Course Course { get; set; } = null!;
    public IReadOnlyList<CourseMaterial> Materials { get; set; } = [];
    public IReadOnlyList<Assignment> Assignments { get; set; } = [];
    public IReadOnlyList<ApplicationUser> Students { get; set; } = [];
    public bool IsTeacher { get; set; }
    public bool IsAdmin { get; set; }
    public Dictionary<Guid, SubmissionStatus> SubmissionStatuses { get; set; } = new();
}
