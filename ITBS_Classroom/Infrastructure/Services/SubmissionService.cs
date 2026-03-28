using ITBS_Classroom.Application.Interfaces.Services;
using ITBS_Classroom.Domain.Enums;
using ITBS_Classroom.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Infrastructure.Services;

public class SubmissionService : ISubmissionService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".ppt",
        ".pptx"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public SubmissionService(ApplicationDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    public async Task<(bool IsAllowed, string Message)> ValidateSubmissionDeadlineAsync(Guid assignmentId, DateTime submittedAtUtc, CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.Assignments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);

        if (assignment is null)
        {
            return (false, "Assignment not found.");
        }

        if (submittedAtUtc > assignment.DeadlineUtc)
        {
            return (false, "Deadline passed. Submission not allowed.");
        }

        return (true, "Submission allowed.");
    }

    public async Task<string> SaveSubmissionFileAsync(IFormFile file, string studentId, Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("File type not allowed.");
        }

        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "submissions", assignmentId.ToString(), studentId);
        Directory.CreateDirectory(uploadsRoot);

        var safeFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(uploadsRoot, safeFileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return Path.Combine("uploads", "submissions", assignmentId.ToString(), studentId, safeFileName).Replace("\\", "/");
    }
}
