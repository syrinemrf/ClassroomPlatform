namespace ITBS_Classroom.Domain.Entities;

public class GroupStudent
{
    public Guid GroupId { get; set; }
    public ClassGroup Group { get; set; } = null!;

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
