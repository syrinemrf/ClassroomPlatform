namespace ITBS_Classroom.Domain.Entities;

public class CalendarEvent
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }
    public ClassGroup Group { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool IsExam { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
