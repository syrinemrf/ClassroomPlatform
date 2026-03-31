using System.ComponentModel.DataAnnotations;

namespace ITBS_Classroom.Models.ViewModels;

public class SubmitAssignmentViewModel
{
    [Required]
    public Guid AssignmentId { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}
