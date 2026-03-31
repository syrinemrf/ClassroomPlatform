using ITBS_Classroom.Application.Interfaces.Services;
using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models;
using ITBS_Classroom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class SubmissionsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public SubmissionsController(ApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    // ?? All submissions for an assignment (Teacher / Admin) ??????????????????

    [Authorize(Roles = ApplicationRoles.Teacher + "," + ApplicationRoles.Admin)]
    [HttpGet]
    public async Task<IActionResult> ByAssignment(Guid assignmentId, CancellationToken ct)
    {
        var assignment = await _db.Assignments
            .Include(a => a.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);
        if (assignment is null) return NotFound();

        var submissions = await _db.Submissions
            .Include(s => s.Student)
            .Include(s => s.Grade)
            .Where(s => s.AssignmentId == assignmentId)
            .OrderByDescending(s => s.SubmittedAtUtc)
            .ToListAsync(ct);

        ViewBag.Assignment = assignment;
        return View(submissions);
    }

    // ?? Grade a submission ???????????????????????????????????????????????????

    [Authorize(Roles = ApplicationRoles.Teacher + "," + ApplicationRoles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Grade(GradeSubmissionViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(ByAssignment),
                new { assignmentId = (await _db.Submissions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == model.SubmissionId, ct))?.AssignmentId });

        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        var submission = await _db.Submissions
            .Include(s => s.Assignment)
            .Include(s => s.Grade)
            .FirstOrDefaultAsync(s => s.Id == model.SubmissionId, ct);
        if (submission is null) return NotFound();

        if (submission.Grade is null)
        {
            submission.Grade = new Grade
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                TeacherId = teacherId,
                Score = model.Score,
                Feedback = model.Feedback,
                GradedAtUtc = DateTime.UtcNow
            };
            await _db.Grades.AddAsync(submission.Grade, ct);
        }
        else
        {
            submission.Grade.Score = model.Score;
            submission.Grade.Feedback = model.Feedback;
            submission.Grade.GradedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyCourseStudentsAsync(submission.Assignment.CourseId,
            "Note publiee pour : " + submission.Assignment.Title,
            NotificationType.GradePublished, gradeId: submission.Grade.Id, cancellationToken: ct);

        TempData["Success"] = "Note enregistree.";
        return RedirectToAction(nameof(ByAssignment), new { assignmentId = submission.AssignmentId });
    }

    // ?? Download a submission file (Teacher / Admin) ?????????????????????????

    [Authorize(Roles = ApplicationRoles.Teacher + "," + ApplicationRoles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Download(Guid submissionId, CancellationToken ct)
    {
        var sub = await _db.Submissions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct);
        if (sub is null) return NotFound();

        var physical = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
            sub.FilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(physical)) return NotFound();
        return PhysicalFile(physical, sub.ContentType, sub.FileName);
    }

    // ?? Student's own grades ?????????????????????????????????????????????????

    [Authorize(Roles = ApplicationRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> MyGrades(CancellationToken ct)
    {
        var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        var submissions = await _db.Submissions
            .Include(s => s.Assignment).ThenInclude(a => a.Course)
            .Include(s => s.Grade)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.SubmittedAtUtc)
            .ToListAsync(ct);

        return View(submissions);
    }
}
