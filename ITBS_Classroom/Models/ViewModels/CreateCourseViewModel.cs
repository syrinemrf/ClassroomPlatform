using System.ComponentModel.DataAnnotations;

namespace ITBS_Classroom.Models.ViewModels;

public class CreateCourseViewModel
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(100)]
    public string Section { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string TeacherId { get; set; } = string.Empty;

    public string ColorTheme { get; set; } = "#1a73e8";
}
