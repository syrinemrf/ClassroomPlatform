using Microsoft.AspNetCore.Identity;

namespace ITBS_Classroom.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfileImagePath { get; set; }

    public string FullName => $"{FirstName} {LastName}";
    public string Initials => $"{(FirstName.Length > 0 ? FirstName[0] : '?')}{(LastName.Length > 0 ? LastName[0] : '?')}".ToUpper();

    // Navigation: courses this teacher teaches
    public ICollection<Course> TaughtCourses { get; set; } = new List<Course>();

    // Navigation: enrollments as a student
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();

    // Navigation: assignments created by this teacher
    public ICollection<Assignment> CreatedAssignments { get; set; } = new List<Assignment>();

    // Navigation: submissions by this student
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    // Navigation: grades given by this teacher
    public ICollection<Grade> GradesGiven { get; set; } = new List<Grade>();

    // Navigation: notifications received
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    // Navigation: message thread participations
    public ICollection<MessageThreadParticipant> MessageThreadParticipants { get; set; } = new List<MessageThreadParticipant>();

    // Navigation: messages sent
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
}
