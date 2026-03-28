using ITBS_Classroom.Domain.Entities;
using ITBS_Classroom.Domain.Enums;
using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ITBS_Classroom.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);
        ViewBag.Users = users;
        return View(new CreatePlatformUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreatePlatformUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Users = await _dbContext.Users.AsNoTracking().ToListAsync();
            return View("Users", model);
        }

        if (model.Role is not (ApplicationRoles.Teacher or ApplicationRoles.Student))
        {
            var isFr = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr";
            ModelState.AddModelError(nameof(model.Role), isFr ? "Rôle invalide." : "Invalid role.");
            ViewBag.Users = await _dbContext.Users.AsNoTracking().ToListAsync();
            return View("Users", model);
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
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.Users = await _dbContext.Users.AsNoTracking().ToListAsync();
            return View("Users", model);
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> Groups(CancellationToken cancellationToken)
    {
        var groups = await _dbContext.Groups
            .Include(x => x.Teacher)
            .Include(x => x.Students)
            .ThenInclude(x => x.Student)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var teachers = (await _userManager.GetUsersInRoleAsync(ApplicationRoles.Teacher))
            .Select(x => new SelectListItem($"{x.FirstName} {x.LastName} ({x.Email})", x.Id))
            .ToList();

        var students = (await _userManager.GetUsersInRoleAsync(ApplicationRoles.Student))
            .Select(x => new SelectListItem($"{x.FirstName} {x.LastName} ({x.Email})", x.Id))
            .ToList();

        ViewBag.Groups = groups;
        ViewBag.Teachers = teachers;
        ViewBag.Students = students;

        return View(new CreateGroupViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(CreateGroupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await Groups(default);
        }

        var group = new ClassGroup
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            Description = model.Description,
            TeacherId = model.TeacherId
        };

        await _dbContext.Groups.AddAsync(group);
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Groups));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStudentToGroup(Guid groupId, string studentId)
    {
        var exists = await _dbContext.GroupStudents.AnyAsync(x => x.GroupId == groupId && x.StudentId == studentId);
        if (!exists)
        {
            await _dbContext.GroupStudents.AddAsync(new GroupStudent
            {
                GroupId = groupId,
                StudentId = studentId,
                JoinedAtUtc = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Groups));
    }
}
