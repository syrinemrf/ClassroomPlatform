using System.ComponentModel.DataAnnotations;

namespace ITBS_Classroom.Models.ViewModels;

public class GradeSubmissionViewModel
{
    [Required]
    public Guid SubmissionId { get; set; }

    [Range(0, 1000)]
    public decimal Score { get; set; }

    [StringLength(4000)]
    public string? Feedback { get; set; }
}
