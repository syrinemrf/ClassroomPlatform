using System.ComponentModel.DataAnnotations;

namespace ITBS_Classroom.Models.Messages;

public class SendMessageViewModel
{
    [Required]
    public Guid ThreadId { get; set; }

    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;
}
