using Microsoft.EntityFrameworkCore;
using EventManageApp.Models;

namespace EventManageApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Scaner> Scaners { get; set; }
    public DbSet<EventManageApp.Models.Task> Tasks { get; set; }
    public DbSet<EventManageApp.Models.TaskSubmission> TaskSubmissions { get; set; }
    public DbSet<Coupon> Coupons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>().HasKey(a => a.Id);
        modelBuilder.Entity<User>().HasBaseType<Account>();
        modelBuilder.Entity<Scaner>().HasBaseType<Account>();

        // Configure defaults
        modelBuilder.Entity<EventManageApp.Models.Task>().Property(t => t.Points).HasDefaultValue(0);
        modelBuilder.Entity<EventManageApp.Models.TaskSubmission>().Property(s => s.IsApproved).HasDefaultValue(false);
    }
}
