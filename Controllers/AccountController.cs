using Microsoft.AspNetCore.Mvc;
using EventManageApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManageApp.Controllers;

public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly EventManageApp.Data.ApplicationDbContext _db;

    public AccountController(ILogger<AccountController> logger, EventManageApp.Data.ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            _logger.LogInformation("Login attempt for user: {Login}", model.Login);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == model.Login && u.Password == model.Password);

            if (user != null)
            {
                // Store username in session
                HttpContext.Session.SetString("Username", user.Login);

                if (string.Equals(user.Login, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    HttpContext.Session.SetString("IsAdmin", "true");
                    return RedirectToAction("Index", "Admin");
                }

                // regular users go to tasks
                HttpContext.Session.SetString("IsAdmin", "false");
                return RedirectToAction("Index", "User");
            }

            ModelState.AddModelError("", "Invalid login or password");
        }

        return View(model);
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        _logger.LogInformation("User logged out");
        return RedirectToAction("Login");
    }
}
