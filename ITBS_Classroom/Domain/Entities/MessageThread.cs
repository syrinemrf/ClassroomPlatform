using ITBS_Classroom.Domain.Enums;

namespace ITBS_Classroom.Domain.Entities;

public class MessageThread
{
    public Guid Id { get; set; }
    public MessageThreadType ThreadType { get; set; } = MessageThreadType.Direct;

    public Guid? AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<MessageThreadParticipant> Participants { get; set; } = new List<MessageThreadParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
