namespace ITBS_Classroom.Models;

/// <summary>
/// Represents a university course (the central entity, like a Google Classroom class).
/// Admin creates courses and assigns a teacher and students.
/// </summary>
public class Course
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ColorTheme { get; set; } = "#1a73e8";

    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser Teacher { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
    public ICollection<CourseMaterial> Materials { get; set; } = new List<CourseMaterial>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<MessageThread> MessageThreads { get; set; } = new List<MessageThread>();
}
