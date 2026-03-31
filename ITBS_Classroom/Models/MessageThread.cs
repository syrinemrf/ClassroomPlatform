namespace ITBS_Classroom.Models;

public class MessageThread
{
    public Guid Id { get; set; }
    public MessageThreadType ThreadType { get; set; } = MessageThreadType.Direct;

    public Guid? CourseId { get; set; }
    public Course? Course { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<MessageThreadParticipant> Participants { get; set; } = new List<MessageThreadParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
