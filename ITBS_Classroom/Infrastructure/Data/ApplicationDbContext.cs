using ITBS_Classroom.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClassGroup> Groups => Set<ClassGroup>();
    public DbSet<GroupStudent> GroupStudents => Set<GroupStudent>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<MessageThread> MessageThreads => Set<MessageThread>();
    public DbSet<MessageThreadParticipant> MessageThreadParticipants => Set<MessageThreadParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
