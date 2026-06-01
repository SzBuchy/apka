using System;
using System.ComponentModel.DataAnnotations;

namespace EventManageApp.Models;

public class Coupon
{
    public int Id { get; set; }
    
    [Required]
    public string SerialNumber { get; set; } = string.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty; // e.g., "Lunch Coupon", "Dinner Coupon"
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public string? ScannedBy { get; set; } // Login of the scaner who processed it
}
