using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ITBS_Classroom.Models.Assignments;

public class AssignmentCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime DeadlineUtc { get; set; }

    [Required]
    public Guid GroupId { get; set; }

    public Guid? CourseId { get; set; }
    public IFormFile? Attachment { get; set; }
}
