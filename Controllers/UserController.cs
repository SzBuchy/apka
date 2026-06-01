using Microsoft.AspNetCore.Mvc;
using EventManageApp.Models;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace EventManageApp.Controllers;

public class UserController : Controller
{
    private readonly ILogger<UserController> _logger;
    private readonly EventManageApp.Data.ApplicationDbContext _db;
    private readonly Cloudinary _cloudinary;

    public UserController(ILogger<UserController> logger, EventManageApp.Data.ApplicationDbContext db, Cloudinary cloudinary)
    {
        _logger = logger;
        _db = db;
        _cloudinary = cloudinary;
    }

    public async Task<IActionResult> Index()
    {
        // Get current user from session
        var username = HttpContext.Session.GetString("Username");
        var user = await _db.Users
            .Include(u => u.TaskSubmissions)
            .FirstOrDefaultAsync(u => u.Login == username);
        
        // Pass user info to view so we can display their points
        ViewBag.CurrentUser = user;
        
        var tasks = await _db.Tasks.ToListAsync();
        return View(tasks);
    }

    public async Task<IActionResult> Details(int id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();
        return View(task);
    }

    public async Task<IActionResult> Coupons()
    {
        var username = HttpContext.Session.GetString("Username");
        var user = await _db.Users
            .Include(u => u.Coupons)
            .FirstOrDefaultAsync(u => u.Login == username);
            
        if (user == null) return RedirectToAction("Login", "Account");
        
        return View(user.Coupons.OrderByDescending(c => c.StartDate).ToList());
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
    public async Task<IActionResult> SubmitAnswer(int taskId, string? answer, IFormFile? submissionFile)
    {
        var username = HttpContext.Session.GetString("Username");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == username);
        
        if (user == null)
        {
            return RedirectToAction("Index");
        }

        var task = await _db.Tasks.FindAsync(taskId);
        
        // Check if answer is required (only if file is NOT required)
        if (string.IsNullOrWhiteSpace(answer) && task?.RequiresPhoto != true)
        {
            ModelState.AddModelError("answer", "Please provide an answer");
            return RedirectToAction("Details", new { id = taskId });
        }

        // Check if file is required
        if (task?.RequiresPhoto == true && submissionFile == null)
        {
            ModelState.AddModelError("submissionFile", "A photo or video is required for this task");
            return RedirectToAction("Details", new { id = taskId });
        }

        string? cloudinaryUrl = null;
        string? fileName = null;
        string? fileContentType = null;

        // Handle file upload if provided
        if (submissionFile != null)
        {
            const long maxFileSize = 50 * 1024 * 1024; // 50MB for videos
            if (submissionFile.Length > maxFileSize)
            {
                ModelState.AddModelError("submissionFile", "File size cannot exceed 50MB");
                return RedirectToAction("Details", new { id = taskId });
            }

            try
            {
                using (var stream = submissionFile.OpenReadStream())
                {
                    var uploadParams = new RawUploadParams()
                    {
                        File = new FileDescription(submissionFile.FileName, stream),
                        Folder = "event_manage_app_submissions"
                    };
                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    
                    if (uploadResult.Error != null)
                    {
                        _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                        ModelState.AddModelError("submissionFile", "Failed to upload file to cloud storage");
                        return RedirectToAction("Details", new { id = taskId });
                    }

                    cloudinaryUrl = uploadResult.SecureUrl.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to Cloudinary");
                ModelState.AddModelError("submissionFile", "An unexpected error occurred during file upload");
                return RedirectToAction("Details", new { id = taskId });
            }
            
            fileName = submissionFile.FileName;
            fileContentType = submissionFile.ContentType;
        }

        var submission = new TaskSubmission
        {
            TaskId = taskId,
            UserId = user.Id,
            Answer = answer ?? string.Empty,
            CloudinaryUrl = cloudinaryUrl,
            FileName = fileName,
            FileContentType = fileContentType,
            SubmittedAt = DateTime.Now
        };

        _db.TaskSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Task submission created for task {TaskId} by user {UserId}", taskId, user.Id);

        return RedirectToAction("Index");
    }
}
