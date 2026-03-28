using System.ComponentModel.DataAnnotations;

namespace ITBS_Classroom.Models.Calendar;

public class CreateEventViewModel
{
    [Required]
    public Guid GroupId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime StartUtc { get; set; }

    [Required]
    public DateTime EndUtc { get; set; }

    public bool IsExam { get; set; }
}
