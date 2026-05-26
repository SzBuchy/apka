using Microsoft.AspNetCore.Mvc;
using EventManageApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManageApp.Controllers;

public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;
    private readonly EventManageApp.Data.ApplicationDbContext _db;

    public AdminController(ILogger<AdminController> logger, EventManageApp.Data.ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    private bool IsAdmin()
    {
        var isAdmin = HttpContext.Session.GetString("IsAdmin");
        return isAdmin == "true";
    }

    public IActionResult Index()
    {
        if (!IsAdmin()) return Forbid();
        return View();
    }

    public async Task<IActionResult> Tasks()
    {
        if (!IsAdmin()) return Forbid();
        var tasks = await _db.Tasks.ToListAsync();
        return View(tasks);
    }

    public IActionResult CreateTask()
    {
        if (!IsAdmin()) return Forbid();
        return View(new Models.Task());
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask(Models.Task task)
    {
        if (!IsAdmin()) return Forbid();
        if (ModelState.IsValid)
        {
            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();
            return RedirectToAction("Tasks");
        }
        return View(task);
    }

    public async Task<IActionResult> EditTask(int id)
    {
        if (!IsAdmin()) return Forbid();

        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();
        return View(task);
    }

    [HttpPost]
    public async Task<IActionResult> EditTask(Models.Task task)
    {
        if (!IsAdmin()) return Forbid();
        if (ModelState.IsValid)
        {
            _db.Tasks.Update(task);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Task {TaskId} updated: {TaskName}", task.Id, task.Name);
            return RedirectToAction("Tasks");
        }

        return View(task);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTask(int id)
    {
        if (!IsAdmin()) return Forbid();

        var task = await _db.Tasks.FindAsync(id);
        if (task != null)
        {
            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Task {TaskId} deleted", id);
        }
        return RedirectToAction("Tasks");
    }

    public async Task<IActionResult> Submissions()
    {
        if (!IsAdmin()) return Forbid();
        var submissions = await _db.TaskSubmissions
            .Include(s => s.User)
            .Include(s => s.Task)
            .ToListAsync();
        return View(submissions);
    }

    public async Task<IActionResult> ReviewSubmission(int id)
    {
        if (!IsAdmin()) return Forbid();
        var submission = await _db.TaskSubmissions.FindAsync(id);
        if (submission == null) return NotFound();

        var task = await _db.Tasks.FindAsync(submission.TaskId);
        var user = await _db.Users.FindAsync(submission.UserId);

        ViewBag.Task = task;
        ViewBag.User = user;

        return View(submission);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveSubmission(int id, string? approvalNotes)
    {
        if (!IsAdmin()) return Forbid();

        var submission = await _db.TaskSubmissions.FindAsync(id);
        if (submission == null) return NotFound();

        // if already approved, do nothing
        if (submission.IsApproved)
        {
            return RedirectToAction("Submissions");
        }

        var task = await _db.Tasks.FindAsync(submission.TaskId);
        var user = await _db.Users.FindAsync(submission.UserId);

        submission.IsApproved = true;
        submission.IsRejected = false;
        submission.RejectionReason = null;
        submission.ApprovedAt = DateTime.Now;
        submission.ApprovedBy = HttpContext.Session.GetString("Username") ?? "admin";
        submission.ApprovalNotes = approvalNotes;

        // Award points to user (only once)
        if (user != null && task != null)
        {
            user.Points += (int)task.Points;
            _db.Users.Update(user);
        }

        _db.TaskSubmissions.Update(submission);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Submission {SubmissionId} approved. User {UserId} awarded {Points} points", 
            id, submission.UserId, task?.Points ?? 0);
        return RedirectToAction("Submissions");
    }

    [HttpPost]
    public async Task<IActionResult> RejectSubmission(int id, string rejectionReason)
    {
        if (!IsAdmin()) return Forbid();
        var submission = await _db.TaskSubmissions.FindAsync(id);
        if (submission != null)
        {
            submission.IsRejected = true;
            submission.RejectionReason = rejectionReason;
            _db.TaskSubmissions.Update(submission);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Submission {SubmissionId} rejected: {Reason}", id, rejectionReason);
        }
        return RedirectToAction("Submissions");
    }

    public async Task<IActionResult> Leaderboard()
    {
        var users = await _db.Users
            .OrderByDescending(u => u.Points)
            .ToListAsync();
        
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> SubmissionFile(int id)
    {
        var submission = await _db.TaskSubmissions.FindAsync(id);
        if (submission == null || submission.SubmissionFile == null)
        {
            return NotFound();
        }

        var content = submission.SubmissionFile;
        var contentType = submission.FileContentType ?? "application/octet-stream";
        var fileName = submission.FileName ?? $"submission_{id}";

        if (contentType.StartsWith("image/"))
        {
            return File(content, contentType);
        }

        return File(content, contentType, fileName);
    }
}
