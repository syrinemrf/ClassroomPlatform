using Microsoft.AspNetCore.Http;

namespace ITBS_Classroom.Application.Interfaces.Services;

public interface ISubmissionService
{
    Task<(bool IsAllowed, string Message)> ValidateSubmissionDeadlineAsync(Guid assignmentId, DateTime submittedAtUtc, CancellationToken cancellationToken = default);
    Task<string> SaveSubmissionFileAsync(IFormFile file, string studentId, Guid assignmentId, CancellationToken cancellationToken = default);
}
