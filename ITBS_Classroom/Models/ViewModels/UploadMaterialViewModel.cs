using System.ComponentModel.DataAnnotations;

namespace ITBS_Classroom.Models.ViewModels;

public class UploadMaterialViewModel
{
    [Required]
    public Guid CourseId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}
