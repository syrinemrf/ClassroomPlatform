namespace ITBS_Classroom.Models;

/// <summary>
/// Many-to-many join entity between Course and Student (ApplicationUser).
/// </summary>
public class CourseEnrollment
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;
}
