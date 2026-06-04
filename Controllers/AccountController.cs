using Microsoft.AspNetCore.Mvc;
using EventManageApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

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
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToUserRole(User.FindFirstValue(ClaimTypes.Role));
        }
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
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, account.Login),
                    new Claim(ClaimTypes.Role, account.Role),
                    new Claim("AccountId", account.Id.ToString()),
                    new Claim("Nickname", account.Nickname ?? account.Login)
                };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                };

                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

                // Store in session for backward compatibility if needed, but claims are better
                HttpContext.Session.SetString("Username", account.Login);
                HttpContext.Session.SetString("IsAdmin", string.Equals(account.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? "true" : "false");
                HttpContext.Session.SetString("IsScaner", string.Equals(account.Role, "Scaner", StringComparison.OrdinalIgnoreCase) ? "true" : "false");

                if (string.IsNullOrEmpty(account.Nickname))
                {
                    return RedirectToAction("SetNickname");
                }

                return RedirectToUserRole(account.Role);
            }

            TempData["LoginFailed"] = true;
        }

        return View(model);
    }

    private IActionResult RedirectToUserRole(string? role)
    {
        var nickname = User.FindFirstValue("Nickname");
        var login = User.Identity?.Name;

        // If nickname is same as login, it might mean it's not set (depending on how we initialized it)
        // But our logic specifically sets Nickname in DB to null initially.
        // The claim is initialized as Nickname ?? Login.
        
        // Let's check the database to be sure if we are in a state that requires setting nickname
        // However, doing a DB check in every redirect might be overkill.
        // Let's rely on the Login POST logic for the first redirect.
        
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Admin");
        if (string.Equals(role, "Scaner", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Scaner");
        
        return RedirectToAction("Index", "User");
    }

    [HttpGet]
    public IActionResult SetNickname()
    {
        if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SetNickname(string nickname)
    {
        if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");

        if (string.IsNullOrWhiteSpace(nickname))
        {
            ModelState.AddModelError("nickname", "Pseudonim nie może być pusty");
            return View();
        }

        var login = User.Identity.Name;
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Login == login);

        if (account != null)
        {
            account.Nickname = nickname;
            await _db.SaveChangesAsync();

            // Rebuild claims from the account object to be 100% sure
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, account.Login),
                new Claim(ClaimTypes.Role, account.Role),
                new Claim("AccountId", account.Id.ToString()),
                new Claim("Nickname", account.Nickname)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

            return RedirectToUserRole(account.Role);
        }

        return RedirectToAction("Login");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("CookieAuth");
        HttpContext.Session.Clear();
        _logger.LogInformation("User logged out");
        return RedirectToAction("Login");
    }
}
