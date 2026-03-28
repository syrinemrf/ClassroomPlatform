using System.ComponentModel.DataAnnotations;

namespace ITBS_Classroom.Models.Submissions;

public class GradeSubmissionViewModel
{
    [Required]
    public Guid SubmissionId { get; set; }

    [Range(0, 100)]
    public decimal Score { get; set; }

    [StringLength(4000)]
    public string? Feedback { get; set; }
}
