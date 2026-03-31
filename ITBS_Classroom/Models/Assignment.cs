namespace ITBS_Classroom.Models;

public class Assignment
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DeadlineUtc { get; set; }
    public int MaxScore { get; set; } = 100;

    public string? AttachmentPath { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentContentType { get; set; }

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser Teacher { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
