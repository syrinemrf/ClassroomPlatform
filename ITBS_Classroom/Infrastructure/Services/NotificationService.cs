using ITBS_Classroom.Application.Interfaces.Services;
using ITBS_Classroom.Domain.Entities;
using ITBS_Classroom.Domain.Enums;
using ITBS_Classroom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task NotifyGroupStudentsAsync(Guid groupId, string message, NotificationType type, Guid? courseId = null, Guid? assignmentId = null, Guid? gradeId = null, CancellationToken cancellationToken = default)
    {
        var studentIds = await _dbContext.GroupStudents
            .Where(x => x.GroupId == groupId)
            .Select(x => x.StudentId)
            .ToListAsync(cancellationToken);

        if (studentIds.Count == 0)
        {
            return;
        }

        var notifications = studentIds.Select(studentId => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = studentId,
            Message = message,
            Type = type,
            CourseId = courseId,
            AssignmentId = assignmentId,
            GradeId = gradeId,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _dbContext.Notifications.AddRangeAsync(notifications, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
