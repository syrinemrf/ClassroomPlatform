using ITBS_Classroom.Domain.Enums;
using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models.Admin;
using ITBS_Classroom.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.IsInRole(ApplicationRoles.Admin))
        {
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _dbContext.Users.CountAsync(cancellationToken),
                TotalGroups = await _dbContext.Groups.CountAsync(cancellationToken),
                TotalCourses = await _dbContext.Courses.CountAsync(cancellationToken)
            };

            return View("AdminDashboard", vm);
        }

        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var groups = await _dbContext.Groups
                .Where(x => x.TeacherId == userId)
                .Include(x => x.Students)
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            var groupIds = groups.Select(x => x.Id).ToList();
            var courses = await _dbContext.Courses
                .Where(x => groupIds.Contains(x.GroupId))
                .Include(x => x.Group)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(8)
                .ToListAsync(cancellationToken);

            var vm = new TeacherDashboardViewModel
            {
                Groups = groups,
                Courses = courses
            };

            return View("TeacherDashboard", vm);
        }

        return View("StudentDashboard");
    }
}
