using Microsoft.AspNetCore.Identity;

namespace ITBS_Classroom.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfileImagePath { get; set; }

    public ICollection<ClassGroup> TeachingGroups { get; set; } = new List<ClassGroup>();
    public ICollection<GroupStudent> GroupMemberships { get; set; } = new List<GroupStudent>();
    public ICollection<Course> CoursesCreated { get; set; } = new List<Course>();
    public ICollection<Assignment> AssignmentsCreated { get; set; } = new List<Assignment>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<Grade> GradesPublished { get; set; } = new List<Grade>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<MessageThreadParticipant> MessageThreadParticipants { get; set; } = new List<MessageThreadParticipant>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<CalendarEvent> CalendarEventsCreated { get; set; } = new List<CalendarEvent>();
}
