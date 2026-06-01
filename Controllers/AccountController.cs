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

            var account = await _db.Accounts.FirstOrDefaultAsync(u => u.Login == model.Login && u.Password == model.Password);

            if (account != null)
            {
                // Store username in session
                HttpContext.Session.SetString("Username", account.Login);

                if (string.Equals(account.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    HttpContext.Session.SetString("IsAdmin", "true");
                    HttpContext.Session.SetString("IsScaner", "false");
                    return RedirectToAction("Index", "Admin");
                }
                
                if (string.Equals(account.Role, "Scaner", StringComparison.OrdinalIgnoreCase))
                {
                    HttpContext.Session.SetString("IsAdmin", "false");
                    HttpContext.Session.SetString("IsScaner", "true");
                    return RedirectToAction("Index", "Scaner");
                }

                // regular users go to tasks
                HttpContext.Session.SetString("IsAdmin", "false");
                HttpContext.Session.SetString("IsScaner", "false");
                return RedirectToAction("Index", "User");
            }

            TempData["LoginFailed"] = true;
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
