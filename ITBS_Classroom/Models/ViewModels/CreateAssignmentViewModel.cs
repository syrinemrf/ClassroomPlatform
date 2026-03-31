using System.ComponentModel.DataAnnotations;

namespace ITBS_Classroom.Models.ViewModels;

public class CreateAssignmentViewModel
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime DeadlineUtc { get; set; }

    [Required]
    public Guid CourseId { get; set; }

    [Range(1, 1000)]
    public int MaxScore { get; set; } = 100;

    public IFormFile? Attachment { get; set; }
}
