namespace ITBS_Classroom.Domain.Entities;

public class Grade
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;

    public decimal Score { get; set; }
    public string? Feedback { get; set; }
    public DateTime GradedAtUtc { get; set; } = DateTime.UtcNow;

    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser Teacher { get; set; } = null!;
}
