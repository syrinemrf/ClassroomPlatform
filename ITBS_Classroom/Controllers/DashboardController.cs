using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models;
using ITBS_Classroom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        if (User.IsInRole(ApplicationRoles.Admin))
        {
            var teachers = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Teacher);
            var students = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Student);
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _db.Users.CountAsync(ct),
                TotalCourses = await _db.Courses.CountAsync(ct),
                TotalAssignments = await _db.Assignments.CountAsync(ct),
                TotalTeachers = teachers.Count,
                TotalStudents = students.Count
            };
            return View("AdminDashboard", vm);
        }

        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var courses = await _db.Courses
                .Where(c => c.TeacherId == userId)
                .Include(c => c.Enrollments)
                .Include(c => c.Assignments)
                .AsNoTracking()
                .OrderBy(c => c.Title)
                .ToListAsync(ct);

            var courseIds = courses.Select(c => c.Id).ToList();
            var pending = await _db.Submissions
                .Where(s => courseIds.Contains(s.Assignment.CourseId) && s.Grade == null)
                .CountAsync(ct);

            var upcoming = await _db.Assignments
                .Where(a => courseIds.Contains(a.CourseId) && a.DeadlineUtc > DateTime.UtcNow)
                .CountAsync(ct);

            var totalStudents = courses.Sum(c => c.Enrollments.Count);

            var vm = new TeacherDashboardViewModel
            {
                Courses = courses,
                TotalStudents = totalStudents,
                PendingSubmissions = pending,
                UpcomingDeadlines = upcoming
            };
            return View("TeacherDashboard", vm);
        }

        // Student
        var enrolledCourses = await _db.Courses
            .Where(c => c.Enrollments.Any(e => e.StudentId == userId))
            .Include(c => c.Teacher)
            .Include(c => c.Assignments)
            .AsNoTracking()
            .OrderBy(c => c.Title)
            .ToListAsync(ct);

        var enrolledCourseIds = enrolledCourses.Select(c => c.Id).ToList();
        var allAssignments = enrolledCourses.SelectMany(c => c.Assignments).ToList();
        var mySubmissions = await _db.Submissions
            .Where(s => s.StudentId == userId && enrolledCourseIds.Contains(s.Assignment.CourseId))
            .Select(s => s.AssignmentId)
            .ToListAsync(ct);

        var upcoming2 = allAssignments
            .Where(a => a.DeadlineUtc > DateTime.UtcNow && !mySubmissions.Contains(a.Id))
            .OrderBy(a => a.DeadlineUtc)
            .Take(5)
            .ToList();

        var studentVm = new StudentDashboardViewModel
        {
            Courses = enrolledCourses,
            PendingAssignments = allAssignments.Count(a => !mySubmissions.Contains(a.Id) && a.DeadlineUtc > DateTime.UtcNow),
            CompletedSubmissions = mySubmissions.Count,
            UpcomingDeadlines = upcoming2
        };
        return View("StudentDashboard", studentVm);
    }
}
