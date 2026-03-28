using System.ComponentModel.DataAnnotations;

namespace ITBS_Classroom.Models.Admin;

public class CreateGroupViewModel
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public string? TeacherId { get; set; }
}
