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
        return User.IsInRole("Admin");
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
        submission.ApprovedBy = User.Identity?.Name ?? "admin";
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
            submission.IsApproved = false; // Ensure it's not approved if rejected
            submission.RejectionReason = rejectionReason;
            _db.TaskSubmissions.Update(submission);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Submission {SubmissionId} rejected: {Reason}", id, rejectionReason);
        }
        return RedirectToAction("Submissions");
    }

    [HttpPost]
    public async Task<IActionResult> ChangeDecision(int id)
    {
        if (!IsAdmin()) return Forbid();
        var submission = await _db.TaskSubmissions
            .Include(s => s.Task)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);
            
        if (submission == null) return NotFound();

        // If it was approved, withdraw points
        if (submission.IsApproved && submission.User != null && submission.Task != null)
        {
            submission.User.Points -= (int)submission.Task.Points;
            _db.Users.Update(submission.User);
        }

        // Reset status to pending
        submission.IsApproved = false;
        submission.IsRejected = false;
        submission.RejectionReason = null;
        submission.ApprovedAt = null;
        submission.ApprovedBy = null;
        submission.ApprovalNotes = null;

        _db.TaskSubmissions.Update(submission);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Decision for submission {SubmissionId} changed. Status reset to Pending.", id);
        return RedirectToAction("ReviewSubmission", new { id = id });
    }

    public async Task<IActionResult> Leaderboard()
    {
        var users = await _db.Users
            .Where(u => u.Role != "Admin")
            .OrderByDescending(u => u.Points)
            .ToListAsync();
        
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUserPoints(int userId, int newPoints)
    {
        if (!IsAdmin()) return Forbid();
        
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        _logger.LogInformation("Admin manually changed points for user {Username} from {OldPoints} to {NewPoints}", 
            user.Login, user.Points, newPoints);

        user.Points = newPoints;
        _db.Users.Update(user);
        await _db.SaveChangesAsync();

        return RedirectToAction("Leaderboard");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateNickname(int userId, string newNickname)
    {
        if (!IsAdmin()) return Forbid();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        _logger.LogInformation("Admin manually changed nickname for user {Login} from {OldNickname} to {NewNickname}",
            user.Login, user.Nickname, newNickname);

        user.Nickname = newNickname;
        _db.Users.Update(user);
        await _db.SaveChangesAsync();

        return RedirectToAction("Leaderboard");
    }

    public async Task<IActionResult> Coupons()
    {
        if (!IsAdmin()) return Forbid();
        var coupons = await _db.Coupons.Include(c => c.User).OrderByDescending(c => c.Id).ToListAsync();
        var users = await _db.Users.Where(u => u.Role != "Admin").ToListAsync();
        ViewBag.Users = users;
        return View(coupons);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCoupon(string title, int userId, DateTime startDate, DateTime endDate)
    {
        if (!IsAdmin()) return Forbid();
        
        if (userId == -1)
        {
            // Create for everyone
            var allUsers = await _db.Users.Where(u => u.Role != "Admin").ToListAsync();
            var coupons = new List<Coupon>();
            
            foreach (var user in allUsers)
            {
                coupons.Add(new Coupon
                {
                    Title = title,
                    UserId = user.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    SerialNumber = "CPN-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    IsUsed = false
                });
            }
            
            _db.Coupons.AddRange(coupons);
            _logger.LogInformation("Admin created global coupon '{Title}' for {Count} users", title, allUsers.Count);
        }
        else
        {
            // Create for single user
            var serial = "CPN-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            
            var coupon = new Coupon
            {
                Title = title,
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                SerialNumber = serial,
                IsUsed = false
            };

            _db.Coupons.Add(coupon);
        }

        await _db.SaveChangesAsync();
        return RedirectToAction("Coupons");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCoupon(int id)
    {
        if (!IsAdmin()) return Forbid();
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon != null)
        {
            _db.Coupons.Remove(coupon);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Coupons");
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
