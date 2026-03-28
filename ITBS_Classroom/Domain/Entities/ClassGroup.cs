namespace ITBS_Classroom.Domain.Entities;

public class ClassGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? TeacherId { get; set; }
    public ApplicationUser? Teacher { get; set; }

    public ICollection<GroupStudent> Students { get; set; } = new List<GroupStudent>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<CalendarEvent> Events { get; set; } = new List<CalendarEvent>();
}
