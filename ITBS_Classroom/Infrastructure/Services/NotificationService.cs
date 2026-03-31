using ITBS_Classroom.Application.Interfaces.Services;
using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db) => _db = db;

    public async Task NotifyCourseStudentsAsync(Guid courseId, string message, NotificationType type,
        Guid? courseId2 = null, Guid? assignmentId = null, Guid? gradeId = null,
        CancellationToken cancellationToken = default)
    {
        var studentIds = await _db.CourseEnrollments
            .Where(e => e.CourseId == courseId)
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken);

        if (studentIds.Count == 0) return;

        var notifications = studentIds.Select(sid => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = sid,
            Message = message,
            Type = type,
            CourseId = courseId2 ?? courseId,
            AssignmentId = assignmentId,
            GradeId = gradeId,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.Notifications.AddRangeAsync(notifications, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
