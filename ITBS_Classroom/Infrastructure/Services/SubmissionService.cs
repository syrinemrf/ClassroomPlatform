using ITBS_Classroom.Application.Interfaces.Services;
using ITBS_Classroom.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Infrastructure.Services;

public class SubmissionService : ISubmissionService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip", ".jpg", ".png"
    };

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public SubmissionService(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<(bool IsAllowed, string Message)> ValidateSubmissionDeadlineAsync(
        Guid assignmentId, DateTime submittedAtUtc, CancellationToken cancellationToken = default)
    {
        var assignment = await _db.Assignments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken);

        if (assignment is null)
            return (false, "Devoir introuvable.");

        if (submittedAtUtc > assignment.DeadlineUtc)
            return (false, "La date limite est dépassée.");

        return (true, string.Empty);
    }

    public async Task<string> SaveSubmissionFileAsync(IFormFile file, string studentId, Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("Type de fichier non autorisé.");

        var folder = Path.Combine(_env.WebRootPath, "uploads", "submissions",
            assignmentId.ToString(), studentId);
        Directory.CreateDirectory(folder);

        var safeFile = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(folder, safeFile);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return Path.Combine("uploads", "submissions", assignmentId.ToString(), studentId, safeFile)
            .Replace("\\", "/");
    }
}
