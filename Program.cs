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

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = "CookieAuth";
    options.DefaultSignInScheme = "CookieAuth";
    options.DefaultChallengeScheme = "CookieAuth";
})
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "UserAuthCookie";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

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
        
        // Only seed if Users/Accounts table is empty
        if (!db.Accounts.Any())
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            string GenerateRandomString(int length) => new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());

            var newUsers = new List<EventManageApp.Models.User>();
            var csvLines = new List<string> { "Login,Password,Role" };

            // Re-add Admin and Scaner for management
            var admin = new EventManageApp.Models.User { Login = "admin", Password = "123", Role = "Admin", Points = 0, IsActive = true };
            db.Users.Add(admin);
            csvLines.Add("admin,123,Admin");

            var scaner = new EventManageApp.Models.Scaner { Login = "scaner", Password = "123", Role = "Scaner" };
            db.Accounts.Add(scaner);
            csvLines.Add("scaner,123,Scaner");

            // Generate 200 random users
            for (int i = 1; i <= 200; i++)
            {
                var login = "user_" + GenerateRandomString(6);
                var password = GenerateRandomString(10);
                
                newUsers.Add(new EventManageApp.Models.User 
                { 
                    Login = login, 
                    Password = password, 
                    Role = "User", 
                    Points = 0, 
                    IsActive = true 
                });
                
                csvLines.Add($"{login},{password},User");
            }
            
            db.Users.AddRange(newUsers);
            db.SaveChanges();

            // Save to CSV file
            File.WriteAllLines("generated_users.csv", csvLines);
            Console.WriteLine("Successfully generated 200 users and saved to generated_users.csv");
        }
        else
        {
            // Ensure admin account exists even if users are already seeded
            var admin = db.Accounts.FirstOrDefault(u => u.Login == "admin");
            if (admin == null)
            {
                db.Users.Add(new EventManageApp.Models.User { Login = "admin", Password = "123", Role = "Admin", Points = 0 });
                db.SaveChanges();
                Console.WriteLine("Re-created missing admin account");
            }
            
            var scaner = db.Accounts.FirstOrDefault(a => a.Login == "scaner");
            if (scaner == null)
            {
                db.Accounts.Add(new EventManageApp.Models.Scaner { Login = "scaner", Password = "123", Role = "Scaner" });
                db.SaveChanges();
                Console.WriteLine("Re-created missing scaner account");
            }
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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
