using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;

// Allow legacy DateTime behavior for PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Use Render's port if available, otherwise fallback to default (or launchSettings.json)
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

// Add Cloudinary
var cloudinaryAccount = new Account(
    builder.Configuration["Cloudinary:CloudName"],
    builder.Configuration["Cloudinary:ApiKey"],
    builder.Configuration["Cloudinary:ApiSecret"]
);
builder.Services.AddSingleton(new Cloudinary(cloudinaryAccount));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();

// configure EF Core with PostgreSQL (Supabase)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<EventManageApp.Data.ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure()));

// Add Supabase client
builder.Services.AddScoped<Supabase.Client>(_ => 
    new Supabase.Client(
        builder.Configuration["Supabase:Url"]!,
        builder.Configuration["Supabase:Key"],
        new Supabase.SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        }));

var app = builder.Build();

// Apply migrations and seed test users
using (var scope = app.Services.CreateScope())
{
    try 
    {
        var db = scope.ServiceProvider.GetRequiredService<EventManageApp.Data.ApplicationDbContext>();
        db.Database.Migrate();
        
        // Only seed if Users table is empty
        if (!db.Users.Any())
        {
            var testUsers = new List<EventManageApp.Models.User>
            {
                new() { Login = "alice", Password = "pass", Points = 950 },
                new() { Login = "bob", Password = "pass", Points = 850 },
                new() { Login = "charlie", Password = "pass", Points = 720 },
                new() { Login = "diana", Password = "pass", Points = 680 },
                new() { Login = "evan", Password = "pass", Points = 610 },
                new() { Login = "frank", Password = "pass", Points = 550 },
                new() { Login = "grace", Password = "pass", Points = 480 },
                new() { Login = "henry", Password = "pass", Points = 420 },
                new() { Login = "iris", Password = "pass", Points = 350 },
                new() { Login = "jack", Password = "pass", Points = 280 }
            };
            
            db.Users.AddRange(testUsers);
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSession();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
