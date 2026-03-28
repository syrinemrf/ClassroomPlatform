using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ITBS_Classroom.Models.Submissions;

public class SubmissionCreateViewModel
{
    [Required]
    public Guid AssignmentId { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}
