using ITBS_Classroom.Domain.Enums;

namespace ITBS_Classroom.Application.Interfaces.Services;

public interface INotificationService
{
    Task NotifyGroupStudentsAsync(Guid groupId, string message, NotificationType type, Guid? courseId = null, Guid? assignmentId = null, Guid? gradeId = null, CancellationToken cancellationToken = default);
}
