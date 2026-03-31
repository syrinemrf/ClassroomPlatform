using ITBS_Classroom.Application.Interfaces.Services;
using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models;
using ITBS_Classroom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class AssignmentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;
    private readonly ISubmissionService _submissions;
    private readonly INotificationService _notifications;

    public AssignmentsController(ApplicationDbContext db, IFileStorageService files,
        ISubmissionService submissions, INotificationService notifications)
    {
        _db = db;
        _files = files;
        _submissions = submissions;
        _notifications = notifications;
    }

    // ?? Assignment list ??????????????????????????????????????????????????????

    [HttpGet]
    public async Task<IActionResult> Index(Guid? courseId, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        IQueryable<Assignment> q = _db.Assignments
            .Include(a => a.Course).ThenInclude(c => c.Teacher)
            .AsNoTracking();

        if (courseId.HasValue) q = q.Where(a => a.CourseId == courseId.Value);

        if (User.IsInRole(ApplicationRoles.Teacher))
            q = q.Where(a => a.TeacherId == userId);
        else if (User.IsInRole(ApplicationRoles.Student))
            q = q.Where(a => a.Course.Enrollments.Any(e => e.StudentId == userId));

        var list = await q.OrderBy(a => a.DeadlineUtc).ToListAsync(ct);

        if (User.IsInRole(ApplicationRoles.Student))
        {
            var ids = list.Select(a => a.Id).ToList();
            var subs = await _db.Submissions
                .Where(s => s.StudentId == userId && ids.Contains(s.AssignmentId))
                .ToDictionaryAsync(s => s.AssignmentId, s => s.Status, ct);
            ViewBag.SubmissionStatuses = subs;
        }

        ViewBag.CourseId = courseId;
        return View(list);
    }

    // ?? Create assignment (Teacher / Admin) ??????????????????????????????????

    [Authorize(Roles = ApplicationRoles.Teacher + "," + ApplicationRoles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAssignmentViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index), new { courseId = model.CourseId });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var owned = await _db.Courses.AnyAsync(c => c.Id == model.CourseId && c.TeacherId == userId, ct);
            if (!owned) return Forbid();
        }

        string? attachPath = null, attachName = null, attachType = null;
        if (model.Attachment is not null)
        {
            var (p, _) = await _files.SaveAsync(model.Attachment, "assignments/" + model.CourseId, ct);
            attachPath = p;
            attachName = model.Attachment.FileName;
            attachType = model.Attachment.ContentType;
        }

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            Title = model.Title,
            Description = model.Description,
            DeadlineUtc = model.DeadlineUtc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(model.DeadlineUtc, DateTimeKind.Utc)
                : model.DeadlineUtc.ToUniversalTime(),
            MaxScore = model.MaxScore,
            CourseId = model.CourseId,
            TeacherId = userId,
            AttachmentPath = attachPath,
            AttachmentName = attachName,
            AttachmentContentType = attachType,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _db.Assignments.AddAsync(assignment, ct);
        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyCourseStudentsAsync(model.CourseId,
            "Nouveau devoir : " + model.Title,
            NotificationType.AssignmentCreated, assignmentId: assignment.Id, cancellationToken: ct);

        TempData["Success"] = "Devoir cree.";
        return RedirectToAction("Detail", "Courses", new { id = model.CourseId });
    }

    // ?? Submit assignment (Student) ??????????????????????????????????????????

    [Authorize(Roles = ApplicationRoles.Student)]
    [HttpGet]
    public async Task<IActionResult> Submit(Guid assignmentId, CancellationToken ct)
    {
        var a = await _db.Assignments.Include(x => x.Course)
            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == assignmentId, ct);
        if (a is null) return NotFound();
        ViewBag.Assignment = a;
        return View(new SubmitAssignmentViewModel { AssignmentId = assignmentId });
    }

    [Authorize(Roles = ApplicationRoles.Student)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitAssignmentViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var a2 = await _db.Assignments.Include(x => x.Course)
                .AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.AssignmentId, ct);
            ViewBag.Assignment = a2;
            return View(model);
        }

        var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var now = DateTime.UtcNow;

        var (allowed, msg) = await _submissions.ValidateSubmissionDeadlineAsync(model.AssignmentId, now, ct);
        if (!allowed)
        {
            ModelState.AddModelError(string.Empty, msg);
            var a3 = await _db.Assignments.Include(x => x.Course)
                .AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.AssignmentId, ct);
            ViewBag.Assignment = a3;
            return View(model);
        }

        var path = await _submissions.SaveSubmissionFileAsync(model.File, studentId, model.AssignmentId, ct);
        var existing = await _db.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == model.AssignmentId && s.StudentId == studentId, ct);

        if (existing is null)
        {
            await _db.Submissions.AddAsync(new Submission
            {
                Id = Guid.NewGuid(),
                AssignmentId = model.AssignmentId,
                StudentId = studentId,
                FilePath = path,
                FileName = model.File.FileName,
                ContentType = model.File.ContentType,
                SubmittedAtUtc = now,
                Status = SubmissionStatus.Submitted
            }, ct);
        }
        else
        {
            existing.FilePath = path;
            existing.FileName = model.File.FileName;
            existing.ContentType = model.File.ContentType;
            existing.SubmittedAtUtc = now;
            existing.Status = SubmissionStatus.Submitted;
        }

        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Devoir soumis avec succes.";

        var assignment = await _db.Assignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == model.AssignmentId, ct);
        return RedirectToAction("Detail", "Courses", new { id = assignment?.CourseId });
    }

    // ?? Delete assignment ????????????????????????????????????????????????????

    [Authorize(Roles = ApplicationRoles.Teacher + "," + ApplicationRoles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var a = await _db.Assignments.FindAsync(new object[] { id }, ct);
        if (a is null) return NotFound();
        var cid = a.CourseId;
        _db.Assignments.Remove(a);
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Devoir supprime.";
        return RedirectToAction("Detail", "Courses", new { id = cid });
    }

    // ?? Download attachment ??????????????????????????????????????????????????

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(Guid id, CancellationToken ct)
    {
        var a = await _db.Assignments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null || a.AttachmentPath is null) return NotFound();
        var physical = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
            a.AttachmentPath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(physical)) return NotFound();
        return PhysicalFile(physical, a.AttachmentContentType!, a.AttachmentName!);
    }
}
