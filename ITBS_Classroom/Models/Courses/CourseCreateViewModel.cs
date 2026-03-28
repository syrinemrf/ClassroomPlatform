using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ITBS_Classroom.Models.Courses;

public class CourseCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid GroupId { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}
