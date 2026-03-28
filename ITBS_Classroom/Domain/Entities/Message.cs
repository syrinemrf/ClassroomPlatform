namespace ITBS_Classroom.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }

    public Guid ThreadId { get; set; }
    public MessageThread Thread { get; set; } = null!;

    public string SenderId { get; set; } = string.Empty;
    public ApplicationUser Sender { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
