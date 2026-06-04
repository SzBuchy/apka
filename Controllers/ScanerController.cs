using Microsoft.AspNetCore.Mvc;
using EventManageApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManageApp.Controllers;

public class ScanerController : Controller
{
    private readonly ILogger<ScanerController> _logger;
    private readonly EventManageApp.Data.ApplicationDbContext _db;

    public class CouponStat
    {
        public string Title { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public ScanerController(ILogger<ScanerController> logger, EventManageApp.Data.ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    private bool IsScaner()
    {
        return User.IsInRole("Scaner");
    }

    public async Task<IActionResult> Index()
    {
        if (!IsScaner()) return Forbid();
        
        // Get statistics
        var stats = await _db.Coupons
            .Where(c => c.IsUsed)
            .GroupBy(c => c.Title)
            .Select(g => new CouponStat { Title = g.Key, Count = g.Count() })
            .ToListAsync();
            
        ViewBag.Stats = stats;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ValidateCoupon(string serialNumber)
    {
        if (!IsScaner()) return Forbid();

        var coupon = await _db.Coupons
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.SerialNumber == serialNumber);

        if (coupon == null)
        {
            return Json(new { success = false, message = "Nie znaleziono kuponu!" });
        }

        if (coupon.IsUsed)
        {
            return Json(new { success = false, message = $"Kupon został już wykorzystany o {coupon.UsedAt:yyyy-MM-dd HH:mm} przez {coupon.ScannedBy}" });
        }

        var now = DateTime.Now;
        if (now < coupon.StartDate)
        {
            return Json(new { success = false, message = $"Kupon nie jest jeszcze aktywny! Ważny od {coupon.StartDate:yyyy-MM-dd}" });
        }

        if (now > coupon.EndDate)
        {
            return Json(new { success = false, message = $"Kupon wygasł o {coupon.EndDate:yyyy-MM-dd}" });
        }

        // Validate success - mark as used
        coupon.IsUsed = true;
        coupon.UsedAt = now;
        coupon.ScannedBy = User.Identity?.Name ?? "nieznany";

        _db.Coupons.Update(coupon);
        await _db.SaveChangesAsync();

        return Json(new { 
            success = true, 
            message = "Kupon zweryfikowany pomyślnie!", 
            title = coupon.Title,
            user = coupon.User?.Nickname ?? coupon.User?.Login 
        });
    }
}
