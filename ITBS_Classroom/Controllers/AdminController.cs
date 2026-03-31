using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models;
using ITBS_Classroom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // ?? Users ????????????????????????????????????????????????????????????????

    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken ct)
    {
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.LastName).ToListAsync(ct);
        var userRoles = new Dictionary<string, IList<string>>();
        foreach (var u in users)
            userRoles[u.Id] = await _userManager.GetRolesAsync(u);

        ViewBag.Users = users;
        ViewBag.UserRoles = userRoles;
        return View(new CreatePlatformUserViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreatePlatformUserViewModel model)
    {
        if (!ModelState.IsValid) return await Users(default);
        if (model.Role is not (ApplicationRoles.Teacher or ApplicationRoles.Student))
        {
            ModelState.AddModelError(nameof(model.Role), "Role invalide.");
            return await Users(default);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return await Users(default);
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        TempData["Success"] = $"Utilisateur {user.FullName} cree avec succes.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            await _userManager.DeleteAsync(user);
            TempData["Success"] = "Utilisateur supprime.";
        }
        return RedirectToAction(nameof(Users));
    }

    // ?? Courses ??????????????????????????????????????????????????????????????

    [HttpGet]
    public async Task<IActionResult> Courses(CancellationToken ct)
    {
        var courses = await _db.Courses
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments)
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct);

        var teachers = (await _userManager.GetUsersInRoleAsync(ApplicationRoles.Teacher))
            .OrderBy(t => t.LastName)
            .Select(t => new SelectListItem(t.FullName + " (" + t.Email + ")", t.Id))
            .ToList();

        var students = (await _userManager.GetUsersInRoleAsync(ApplicationRoles.Student))
            .OrderBy(s => s.LastName)
            .Select(s => new SelectListItem(s.FullName + " (" + s.Email + ")", s.Id))
            .ToList();

        ViewBag.Courses = courses;
        ViewBag.Teachers = teachers;
        ViewBag.Students = students;
        return View(new CreateCourseViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(CreateCourseViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await Courses(ct);

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Title = model.Title,
            Section = model.Section,
            Description = model.Description,
            TeacherId = model.TeacherId,
            ColorTheme = model.ColorTheme,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _db.Courses.AddAsync(course, ct);
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Cours cree avec succes.";
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(Guid courseId, CancellationToken ct)
    {
        var course = await _db.Courses.FindAsync(new object[] { courseId }, ct);
        if (course is not null)
        {
            _db.Courses.Remove(course);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "Cours supprime.";
        }
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnrollStudent(Guid courseId, string studentId, CancellationToken ct)
    {
        var exists = await _db.CourseEnrollments
            .AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId, ct);
        if (!exists)
        {
            await _db.CourseEnrollments.AddAsync(new CourseEnrollment
            {
                CourseId = courseId,
                StudentId = studentId,
                EnrolledAtUtc = DateTime.UtcNow
            }, ct);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "Etudiant inscrit.";
        }
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UnenrollStudent(Guid courseId, string studentId, CancellationToken ct)
    {
        var enrollment = await _db.CourseEnrollments
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == studentId, ct);
        if (enrollment is not null)
        {
            _db.CourseEnrollments.Remove(enrollment);
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "Etudiant desincrit.";
        }
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTeacher(Guid courseId, string teacherId, CancellationToken ct)
    {
        var course = await _db.Courses.FindAsync(new object[] { courseId }, ct);
        if (course is not null)
        {
            course.TeacherId = teacherId;
            await _db.SaveChangesAsync(ct);
            TempData["Success"] = "Enseignant assigne.";
        }
        return RedirectToAction(nameof(Courses));
    }
}
