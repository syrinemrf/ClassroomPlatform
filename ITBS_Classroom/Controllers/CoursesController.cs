using ITBS_Classroom.Application.Interfaces.Services;
using ITBS_Classroom.Domain.Entities;
using ITBS_Classroom.Domain.Enums;
using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class CoursesController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;

    public CoursesController(ApplicationDbContext dbContext, IFileStorageService fileStorageService, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        IQueryable<Course> query = _dbContext.Courses
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

        var courses = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

        if (User.IsInRole(ApplicationRoles.Teacher) && userId is not null)
        {
            var groups = await _dbContext.Groups
                .Where(x => x.TeacherId == userId)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(cancellationToken);
            ViewBag.Groups = groups;
        }
        else if (User.IsInRole(ApplicationRoles.Admin))
        {
            var groups = await _dbContext.Groups
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(cancellationToken);
            ViewBag.Groups = groups;
        }

        return View(courses);
    }

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseCreateViewModel model, CancellationToken cancellationToken)
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

        var (relativePath, storedName) = await _fileStorageService.SaveAsync(model.File, Path.Combine("courses", model.GroupId.ToString()), cancellationToken);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = model.Title,
            Description = model.Description,
            GroupId = model.GroupId,
            TeacherId = userId,
            FilePath = relativePath,
            FileName = storedName,
            ContentType = model.File.ContentType,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Courses.AddAsync(course, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var isFr = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr";
        var message = isFr ? "Nouveau cours disponible." : "New course uploaded.";
        await _notificationService.NotifyGroupStudentsAsync(course.GroupId, message, NotificationType.CourseUploaded, courseId: course.Id, cancellationToken: cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Admin},{ApplicationRoles.Student}")]
    [HttpGet]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", course.FilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        return PhysicalFile(physicalPath, course.ContentType, course.FileName);
    }

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (User.IsInRole(ApplicationRoles.Teacher) && !string.IsNullOrWhiteSpace(userId))
        {
            var isTeacherGroup = await _dbContext.Groups.AsNoTracking().AnyAsync(x => x.Id == course.GroupId && x.TeacherId == userId, cancellationToken);
            if (!isTeacherGroup)
            {
                return Forbid();
            }
        }

        var linkedAssignments = await _dbContext.Assignments
            .Where(x => x.CourseId == id)
            .ToListAsync(cancellationToken);

        foreach (var assignment in linkedAssignments)
        {
            assignment.CourseId = null;
        }

        _dbContext.Courses.Remove(course);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}
