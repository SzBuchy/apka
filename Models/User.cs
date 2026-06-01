namespace EventManageApp.Models;

public class User : Account
{
    public int Points { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<TaskSubmission> TaskSubmissions { get; set; } = new List<TaskSubmission>();
    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
}
