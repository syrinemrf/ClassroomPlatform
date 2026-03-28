using ITBS_Classroom.Domain.Entities;
using ITBS_Classroom.Domain.Enums;
using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public MessagesController(ApplicationDbContext dbContext)
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

        var threadIds = await _dbContext.MessageThreadParticipants
            .Where(x => x.UserId == userId)
            .Select(x => x.ThreadId)
            .ToListAsync(cancellationToken);

        var threads = await _dbContext.MessageThreads
            .Include(x => x.Messages.OrderByDescending(m => m.SentAtUtc).Take(1))
            .Where(x => threadIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return View(threads);
    }

    [HttpGet]
    public async Task<IActionResult> Thread(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var isParticipant = await _dbContext.MessageThreadParticipants.AnyAsync(x => x.ThreadId == id && x.UserId == userId, cancellationToken);
        if (!isParticipant)
        {
            return Forbid();
        }

        var thread = await _dbContext.MessageThreads
            .Include(x => x.Messages.OrderBy(m => m.SentAtUtc))
            .ThenInclude(x => x.Sender)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (thread is null)
        {
            return NotFound();
        }

        ViewBag.SendModel = new SendMessageViewModel { ThreadId = id };
        return View(thread);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(SendMessageViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Thread), new { id = model.ThreadId });
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var isParticipant = await _dbContext.MessageThreadParticipants.AnyAsync(x => x.ThreadId == model.ThreadId && x.UserId == userId, cancellationToken);
        if (!isParticipant)
        {
            return Forbid();
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ThreadId = model.ThreadId,
            SenderId = userId,
            Content = model.Content,
            SentAtUtc = DateTime.UtcNow,
            IsRead = false
        };

        await _dbContext.Messages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(Thread), new { id = model.ThreadId });
    }

    [Authorize(Roles = $"{ApplicationRoles.Teacher},{ApplicationRoles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDirectThread(string studentId, CancellationToken cancellationToken)
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId) || string.IsNullOrWhiteSpace(studentId))
        {
            return Unauthorized();
        }

        var existingThread = await _dbContext.MessageThreads
            .Where(x => x.ThreadType == MessageThreadType.Direct)
            .Where(x => x.Participants.Any(p => p.UserId == teacherId))
            .Where(x => x.Participants.Any(p => p.UserId == studentId))
            .FirstOrDefaultAsync(cancellationToken);

        if (existingThread is not null)
        {
            return RedirectToAction(nameof(Thread), new { id = existingThread.Id });
        }

        var thread = new MessageThread
        {
            Id = Guid.NewGuid(),
            ThreadType = MessageThreadType.Direct,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.MessageThreads.AddAsync(thread, cancellationToken);
        await _dbContext.MessageThreadParticipants.AddRangeAsync([
            new MessageThreadParticipant { ThreadId = thread.Id, UserId = teacherId, JoinedAtUtc = DateTime.UtcNow },
            new MessageThreadParticipant { ThreadId = thread.Id, UserId = studentId, JoinedAtUtc = DateTime.UtcNow }
        ], cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Thread), new { id = thread.Id });
    }
}
