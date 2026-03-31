using ITBS_Classroom.Infrastructure.Data;
using ITBS_Classroom.Models;
using ITBS_Classroom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITBS_Classroom.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly ApplicationDbContext _db;

    public MessagesController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var threadIds = await _db.MessageThreadParticipants
            .Where(p => p.UserId == userId)
            .Select(p => p.ThreadId)
            .ToListAsync(ct);

        var threads = await _db.MessageThreads
            .Include(t => t.Participants).ThenInclude(p => p.User)
            .Include(t => t.Messages.OrderByDescending(m => m.SentAtUtc).Take(1))
            .Include(t => t.Course)
            .Where(t => threadIds.Contains(t.Id))
            .OrderByDescending(t => t.Messages.Max(m => (DateTime?)m.SentAtUtc) ?? t.CreatedAtUtc)
            .ToListAsync(ct);

        ViewBag.UserId = userId;
        return View(threads);
    }

    [HttpGet]
    public async Task<IActionResult> Thread(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var isParticipant = await _db.MessageThreadParticipants
            .AnyAsync(p => p.ThreadId == id && p.UserId == userId, ct);
        if (!isParticipant) return Forbid();

        var thread = await _db.MessageThreads
            .Include(t => t.Participants).ThenInclude(p => p.User)
            .Include(t => t.Messages.OrderBy(m => m.SentAtUtc)).ThenInclude(m => m.Sender)
            .Include(t => t.Course)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (thread is null) return NotFound();
        ViewBag.UserId = userId;
        ViewBag.SendModel = new SendMessageViewModel { ThreadId = id };
        return View(thread);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(SendMessageViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Thread), new { id = model.ThreadId });
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var isParticipant = await _db.MessageThreadParticipants
            .AnyAsync(p => p.ThreadId == model.ThreadId && p.UserId == userId, ct);
        if (!isParticipant) return Forbid();

        await _db.Messages.AddAsync(new Message
        {
            Id = Guid.NewGuid(),
            ThreadId = model.ThreadId,
            SenderId = userId,
            Content = model.Content,
            SentAtUtc = DateTime.UtcNow
        }, ct);
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Thread), new { id = model.ThreadId });
    }

    [Authorize(Roles = ApplicationRoles.Teacher + "," + ApplicationRoles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDirectThread(string targetUserId, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        var existing = await _db.MessageThreads
            .Where(t => t.ThreadType == MessageThreadType.Direct
                && t.Participants.Any(p => p.UserId == userId)
                && t.Participants.Any(p => p.UserId == targetUserId))
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return RedirectToAction(nameof(Thread), new { id = existing.Id });

        var thread = new MessageThread
        {
            Id = Guid.NewGuid(),
            ThreadType = MessageThreadType.Direct,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _db.MessageThreads.AddAsync(thread, ct);
        await _db.MessageThreadParticipants.AddRangeAsync([
            new MessageThreadParticipant { ThreadId = thread.Id, UserId = userId },
            new MessageThreadParticipant { ThreadId = thread.Id, UserId = targetUserId }
        ], ct);
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Thread), new { id = thread.Id });
    }
}
