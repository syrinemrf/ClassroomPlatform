using System.ComponentModel.DataAnnotations;

namespace ITBS_Classroom.Application.DTOs.Assignments;

public class CreateAssignmentDto
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
}
