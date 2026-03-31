using ITBS_Classroom.Application.Interfaces.Services;
using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models;
using ITBS_Classroom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class CoursesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;
    private readonly INotificationService _notifications;

    public CoursesController(ApplicationDbContext db, IFileStorageService files,
        INotificationService notifications)
    {
        _db = db;
        _files = files;
        _notifications = notifications;
    }

    // ?? Course list (Classroom-style cards) ??????????????????????????????????

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        IQueryable<Course> q = _db.Courses
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments)
            .AsNoTracking();

        if (User.IsInRole(ApplicationRoles.Teacher))
            q = q.Where(c => c.TeacherId == userId);
        else if (User.IsInRole(ApplicationRoles.Student))
            q = q.Where(c => c.Enrollments.Any(e => e.StudentId == userId));

        var courses = await q.OrderBy(c => c.Title).ToListAsync(ct);
        return View(courses);
    }

    // ?? Course detail ????????????????????????????????????????????????????????

    [HttpGet]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        var course = await _db.Courses
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments).ThenInclude(e => e.Student)
            .Include(c => c.Materials)
            .Include(c => c.Assignments)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course is null) return NotFound();

        var isTeacher = User.IsInRole(ApplicationRoles.Teacher) && course.TeacherId == userId;
        var isAdmin = User.IsInRole(ApplicationRoles.Admin);
        var isStudent = User.IsInRole(ApplicationRoles.Student)
            && course.Enrollments.Any(e => e.StudentId == userId);

        if (!isTeacher && !isAdmin && !isStudent) return Forbid();

        var assignmentIds = course.Assignments.Select(a => a.Id).ToList();
        var statuses = new Dictionary<Guid, SubmissionStatus>();
        if (isStudent)
        {
            var submissions = await _db.Submissions
                .Where(s => s.StudentId == userId && assignmentIds.Contains(s.AssignmentId))
                .ToDictionaryAsync(s => s.AssignmentId, s => s.Status, ct);
            statuses = submissions;
        }

        var vm = new CourseDetailViewModel
        {
            Course = course,
            Materials = course.Materials.OrderByDescending(m => m.CreatedAtUtc).ToList(),
            Assignments = course.Assignments.OrderByDescending(a => a.DeadlineUtc).ToList(),
            Students = course.Enrollments.Select(e => e.Student).ToList(),
            IsTeacher = isTeacher,
            IsAdmin = isAdmin,
            SubmissionStatuses = statuses
        };

        return View(vm);
    }

    // ?? Upload material (Teacher / Admin) ????????????????????????????????????

    [Authorize(Roles = ApplicationRoles.Teacher + "," + ApplicationRoles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadMaterial(UploadMaterialViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Detail), new { id = model.CourseId });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var owned = await _db.Courses.AnyAsync(c => c.Id == model.CourseId && c.TeacherId == userId, ct);
            if (!owned) return Forbid();
        }

        var (path, stored) = await _files.SaveAsync(model.File,
            "materials/" + model.CourseId, ct);

        var material = new CourseMaterial
        {
            Id = Guid.NewGuid(),
            CourseId = model.CourseId,
            Title = model.Title,
            Description = model.Description,
            FilePath = path,
            FileName = model.File.FileName,
            ContentType = model.File.ContentType,
            UploadedById = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _db.CourseMaterials.AddAsync(material, ct);
        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyCourseStudentsAsync(model.CourseId,
            "Nouveau support de cours : " + model.Title,
            NotificationType.CourseUploaded, courseId2: model.CourseId, cancellationToken: ct);

        TempData["Success"] = "Support publie.";
        return RedirectToAction(nameof(Detail), new { id = model.CourseId });
    }

    // ?? Download material ????????????????????????????????????????????????????

    [HttpGet]
    public async Task<IActionResult> Download(Guid materialId, CancellationToken ct)
    {
        var mat = await _db.CourseMaterials.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == materialId, ct);
        if (mat is null) return NotFound();

        var physical = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
            mat.FilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(physical)) return NotFound();

        return PhysicalFile(physical, mat.ContentType, mat.FileName);
    }

    // ?? Delete material ??????????????????????????????????????????????????????

    [Authorize(Roles = ApplicationRoles.Teacher + "," + ApplicationRoles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMaterial(Guid materialId, Guid courseId, CancellationToken ct)
    {
        var mat = await _db.CourseMaterials.FindAsync(new object[] { materialId }, ct);
        if (mat is not null)
        {
            _db.CourseMaterials.Remove(mat);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "Support supprime.";
        }
        return RedirectToAction(nameof(Detail), new { id = courseId });
    }
}
