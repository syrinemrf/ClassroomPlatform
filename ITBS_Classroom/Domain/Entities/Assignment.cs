namespace ITBS_Classroom.Domain.Entities;

public class Assignment
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DeadlineUtc { get; set; }

    public string? AttachmentPath { get; set; }
    public string? AttachmentName { get; set; }
    public string? AttachmentContentType { get; set; }

    public Guid GroupId { get; set; }
    public ClassGroup Group { get; set; } = null!;

    public Guid? CourseId { get; set; }
    public Course? Course { get; set; }

    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser Teacher { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<MessageThread> MessageThreads { get; set; } = new List<MessageThread>();
}
