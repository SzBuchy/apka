using Microsoft.AspNetCore.Mvc;
using EventManageApp.Models;

namespace EventManageApp.Controllers;

public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;

    public AccountController(ILogger<AccountController> logger)
    {
        _logger = logger;
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            _logger.LogInformation("Login attempt for user: {Login}", model.Login);

            // Store username in session
            HttpContext.Session.SetString("Username", model.Login);

            // simple placeholder: treat anyone with login "admin" as administrator
            if (string.Equals(model.Login, "admin", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                return RedirectToAction("Index", "Admin");
            }

            // regular users go to tasks
            HttpContext.Session.SetString("IsAdmin", "false");
            return RedirectToAction("Index", "User");
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
