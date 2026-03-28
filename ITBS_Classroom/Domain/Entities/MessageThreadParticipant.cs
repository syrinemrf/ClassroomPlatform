namespace ITBS_Classroom.Domain.Entities;

public class MessageThreadParticipant
{
    public Guid ThreadId { get; set; }
    public MessageThread Thread { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
