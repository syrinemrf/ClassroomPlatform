using ITBS_Classroom.Application.Interfaces.Services;
using ITBS_Classroom.Domain.Entities;
using ITBS_Classroom.Domain.Enums;
using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models.Submissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class SubmissionsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public SubmissionsController(ApplicationDbContext dbContext, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Admin}")]
    [HttpGet]
    public async Task<IActionResult> ByAssignment(Guid assignmentId, CancellationToken cancellationToken)
    {
        var submissions = await _dbContext.Submissions
            .Include(x => x.Student)
            .Include(x => x.Grade)
            .Where(x => x.AssignmentId == assignmentId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        ViewBag.AssignmentId = assignmentId;
        return View(submissions);
    }

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Grade(GradeSubmissionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var sub = await _dbContext.Submissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.SubmissionId, cancellationToken);
            if (sub is null)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(ByAssignment), new { assignmentId = sub.AssignmentId });
        }

        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Unauthorized();
        }

        var submission = await _dbContext.Submissions
            .Include(x => x.Assignment)
            .Include(x => x.Grade)
            .FirstOrDefaultAsync(x => x.Id == model.SubmissionId, cancellationToken);

        if (submission is null)
        {
            return NotFound();
        }

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
            await _dbContext.Grades.AddAsync(submission.Grade, cancellationToken);
        }
        else
        {
            submission.Grade.Score = model.Score;
            submission.Grade.Feedback = model.Feedback;
            submission.Grade.GradedAtUtc = DateTime.UtcNow;
            submission.Grade.TeacherId = teacherId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var isFr = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr";
        await _notificationService.NotifyGroupStudentsAsync(
            submission.Assignment.GroupId,
            isFr ? "Note publiée." : "Grade published.",
            NotificationType.GradePublished,
            gradeId: submission.Grade.Id,
            cancellationToken: cancellationToken);

        return RedirectToAction(nameof(ByAssignment), new { assignmentId = submission.AssignmentId });
    }

    [Authorize(Roles = ApplicationRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> MyGrades(CancellationToken cancellationToken)
    {
        var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized();
        }

        var submissions = await _dbContext.Submissions
            .Include(x => x.Assignment)
            .Include(x => x.Grade)
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

        return View(submissions);
    }
}
