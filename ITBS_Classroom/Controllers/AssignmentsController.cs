using ITBS_Classroom.Application.Interfaces.Services;
using ITBS_Classroom.Domain.Entities;
using ITBS_Classroom.Domain.Enums;
using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models.Assignments;
using ITBS_Classroom.Models.Submissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class AssignmentsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;
    private readonly ISubmissionService _submissionService;
    private readonly INotificationService _notificationService;

    public AssignmentsController(
        ApplicationDbContext dbContext,
        IFileStorageService fileStorageService,
        ISubmissionService submissionService,
        INotificationService notificationService)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
        _submissionService = submissionService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        IQueryable<Assignment> query = _dbContext.Assignments
            .Include(x => x.Group)
            .Include(x => x.Teacher)
            .AsNoTracking();

        if (User.IsInRole(ApplicationRoles.Teacher) && userId is not null)
        {
            query = query.Where(x => x.Group.TeacherId == userId);
        }

        if (User.IsInRole(ApplicationRoles.Student) && userId is not null)
        {
            var groupIds = _dbContext.GroupStudents.Where(x => x.StudentId == userId).Select(x => x.GroupId);
            query = query.Where(x => groupIds.Contains(x.GroupId));
        }

        var assignments = await query.OrderBy(x => x.DeadlineUtc).ToListAsync(cancellationToken);

        if (User.IsInRole(ApplicationRoles.Teacher) && userId is not null)
        {
            ViewBag.Groups = await _dbContext.Groups
                .Where(x => x.TeacherId == userId)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(cancellationToken);
        }
        else if (User.IsInRole(ApplicationRoles.Admin))
        {
            ViewBag.Groups = await _dbContext.Groups
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(cancellationToken);
        }

        if (User.IsInRole(ApplicationRoles.Student) && userId is not null)
        {
            var assignmentIds = assignments.Select(x => x.Id).ToList();
            var submissions = await _dbContext.Submissions
                .Where(x => x.StudentId == userId && assignmentIds.Contains(x.AssignmentId))
                .ToDictionaryAsync(x => x.AssignmentId, x => x.Status, cancellationToken);
            ViewBag.SubmissionStatuses = submissions;
        }

        return View(assignments);
    }

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssignmentCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var group = await _dbContext.Groups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.GroupId, cancellationToken);
        if (group is null)
        {
            return NotFound();
        }

        if (User.IsInRole(ApplicationRoles.Teacher) && group.TeacherId != userId)
        {
            return Forbid();
        }

        if (model.CourseId.HasValue)
        {
            var courseExistsForGroup = await _dbContext.Courses.AsNoTracking()
                .AnyAsync(x => x.Id == model.CourseId.Value && x.GroupId == model.GroupId, cancellationToken);
            if (!courseExistsForGroup)
            {
                return BadRequest();
            }
        }

        string? attachmentPath = null;
        string? attachmentName = null;
        string? attachmentContentType = null;

        if (model.Attachment is not null)
        {
            var saveResult = await _fileStorageService.SaveAsync(model.Attachment, Path.Combine("assignments", model.GroupId.ToString()), cancellationToken);
            attachmentPath = saveResult.RelativePath;
            attachmentName = saveResult.StoredFileName;
            attachmentContentType = model.Attachment.ContentType;
        }

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            Title = model.Title,
            Description = model.Description,
            DeadlineUtc = model.DeadlineUtc,
            GroupId = model.GroupId,
            CourseId = model.CourseId,
            TeacherId = userId,
            AttachmentPath = attachmentPath,
            AttachmentName = attachmentName,
            AttachmentContentType = attachmentContentType,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Assignments.AddAsync(assignment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var isFr = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr";
        var message = isFr ? "Nouveau devoir créé." : "New assignment created.";
        await _notificationService.NotifyGroupStudentsAsync(assignment.GroupId, message, NotificationType.AssignmentCreated, assignmentId: assignment.Id, cancellationToken: cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ApplicationRoles.Student)]
    [HttpGet]
    public IActionResult Submit(Guid assignmentId)
    {
        return View(new SubmissionCreateViewModel { AssignmentId = assignmentId });
    }

    [Authorize(Roles = ApplicationRoles.Student)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmissionCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var deadlineValidation = await _submissionService.ValidateSubmissionDeadlineAsync(model.AssignmentId, now, cancellationToken);
        if (!deadlineValidation.IsAllowed)
        {
            ModelState.AddModelError(string.Empty, deadlineValidation.Message);
            return View(model);
        }

        var existingSubmission = await _dbContext.Submissions
            .FirstOrDefaultAsync(x => x.AssignmentId == model.AssignmentId && x.StudentId == studentId, cancellationToken);

        var relativePath = await _submissionService.SaveSubmissionFileAsync(model.File, studentId, model.AssignmentId, cancellationToken);

        if (existingSubmission is null)
        {
            existingSubmission = new Submission
            {
                Id = Guid.NewGuid(),
                AssignmentId = model.AssignmentId,
                StudentId = studentId,
                FilePath = relativePath,
                FileName = Path.GetFileName(relativePath),
                ContentType = model.File.ContentType,
                SubmittedAtUtc = now,
                Status = SubmissionStatus.Submitted
            };

            await _dbContext.Submissions.AddAsync(existingSubmission, cancellationToken);
        }
        else
        {
            existingSubmission.FilePath = relativePath;
            existingSubmission.FileName = Path.GetFileName(relativePath);
            existingSubmission.ContentType = model.File.ContentType;
            existingSubmission.SubmittedAtUtc = now;
            existingSubmission.Status = SubmissionStatus.Submitted;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.Assignments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (assignment is null)
        {
            return NotFound();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (User.IsInRole(ApplicationRoles.Teacher) && !string.IsNullOrWhiteSpace(userId))
        {
            var isTeacherGroup = await _dbContext.Groups.AsNoTracking().AnyAsync(x => x.Id == assignment.GroupId && x.TeacherId == userId, cancellationToken);
            if (!isTeacherGroup)
            {
                return Forbid();
            }
        }

        _dbContext.Assignments.Remove(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}
