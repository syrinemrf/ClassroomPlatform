namespace ITBS_Classroom.Models;

public class Notification
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.General;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? CourseId { get; set; }
    public Guid? AssignmentId { get; set; }
    public Guid? GradeId { get; set; }
}
