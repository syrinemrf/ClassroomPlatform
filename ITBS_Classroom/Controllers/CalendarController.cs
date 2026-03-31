using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models;
using ITBS_Classroom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class CalendarController : Controller
{
    private readonly ApplicationDbContext _db;

    public CalendarController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Index() => View();

    // ?? JSON feed for FullCalendar ????????????????????????????????????????????

    [HttpGet]
    public async Task<IActionResult> Events(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        IQueryable<Assignment> q = _db.Assignments
            .Include(a => a.Course)
            .AsNoTracking();

        if (User.IsInRole(ApplicationRoles.Teacher))
            q = q.Where(a => a.TeacherId == userId);
        else if (User.IsInRole(ApplicationRoles.Student))
            q = q.Where(a => a.Course.Enrollments.Any(e => e.StudentId == userId));

        var assignments = await q.OrderBy(a => a.DeadlineUtc).ToListAsync(ct);

        var events = assignments.Select(a =>
        {
            var daysLeft = (a.DeadlineUtc - DateTime.UtcNow).TotalDays;
            string color = daysLeft switch
            {
                < 0 => "#ea4335",    // overdue — red
                < 1 => "#ff6d00",    // due today — orange
                < 3 => "#fbbc04",    // due in < 3 days — yellow
                < 7 => "#1a73e8",    // due in < 7 days — blue
                _ => "#34a853"       // ample time — green
            };

            return new CalendarEventDto
            {
                Title = a.Title + " — " + a.Course.Title,
                Start = a.DeadlineUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Color = color,
                CourseName = a.Course.Title
            };
        });

        return Json(events);
    }
}
