using ITBS_Classroom.Domain.Entities;
using ITBS_Classroom.Domain.Enums;
using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class CalendarController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public CalendarController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        IQueryable<CalendarEvent> query = _dbContext.CalendarEvents.Include(x => x.Group).AsNoTracking();

        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            query = query.Where(x => x.Group.TeacherId == userId);
            ViewBag.Groups = await _dbContext.Groups
                .Where(x => x.TeacherId == userId)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(cancellationToken);
        }
        else if (User.IsInRole(ApplicationRoles.Student))
        {
            var groupIds = _dbContext.GroupStudents.Where(x => x.StudentId == userId).Select(x => x.GroupId);
            query = query.Where(x => groupIds.Contains(x.GroupId));
        }

        var events = await query.OrderBy(x => x.StartUtc).ToListAsync(cancellationToken);
        return View(events);
    }

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEventViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.EndUtc < model.StartUtc)
        {
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var ev = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            GroupId = model.GroupId,
            Title = model.Title,
            Description = model.Description,
            StartUtc = model.StartUtc,
            EndUtc = model.EndUtc,
            IsExam = model.IsExam,
            CreatedById = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.CalendarEvents.AddAsync(ev, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}
