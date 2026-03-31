using ITBS_Classroom.Models;

namespace ITBS_Classroom.Application.Interfaces.Services;

public interface INotificationService
{
    Task NotifyCourseStudentsAsync(Guid courseId, string message, NotificationType type,
        Guid? courseId2 = null, Guid? assignmentId = null, Guid? gradeId = null,
        CancellationToken cancellationToken = default);
}
