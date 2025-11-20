using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1;
using WebApplication1.Data;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Register ApplicationDbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Identity with Email as Username configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
    options.User.RequireUniqueEmail = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? Microsoft.AspNetCore.Http.CookieSecurePolicy.None
        : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
});

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireHR", policy => policy.RequireRole("HR"));
    options.AddPolicy("RequireProgrammeCoordinator", policy => policy.RequireRole("ProgrammeCoordinator"));
    options.AddPolicy("RequireAcademicManager", policy => policy.RequireRole("AcademicManager"));
    options.AddPolicy("RequireLecturer", policy => policy.RequireRole("Lecturer"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// NUCLEAR OPTION: Completely reset and recreate database
await ResetAndRecreateDatabase(app);

app.Run();

async Task ResetAndRecreateDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        Console.WriteLine("🚀 Starting database reset and recreation...");

        // Step 1: Delete existing database
        Console.WriteLine("🗑️  Deleting existing database...");
        await context.Database.EnsureDeletedAsync();
        Console.WriteLine("✅ Database deleted.");

        // Step 2: Create new database
        Console.WriteLine("🔄 Creating new database...");
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("✅ Database created.");

        // Step 3: Wait for tables to be ready
        Console.WriteLine("⏳ Waiting for tables to initialize...");
        await Task.Delay(3000);

        // Step 4: Seed data
        Console.WriteLine("🌱 Seeding initial data...");
        await SeedData.InitializeAsync(context, userManager, roleManager);

        Console.WriteLine("🎉 Database setup completed successfully!");
        Console.WriteLine("📧 You can now login with:");
        Console.WriteLine("   HR: hr@iet.com / Password123!");
        Console.WriteLine("   Lecturer: lecturer@iet.com / Password123!");
        Console.WriteLine("   Coordinator: coordinator@iet.com / Password123!");
        Console.WriteLine("   Manager: manager@iet.com / Password123!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Database initialization failed");
        Console.WriteLine($"❌ Error: {ex.Message}");

        // Don't throw - let the application start anyway
        Console.WriteLine("⚠️  Application will start but database may not be fully initialized.");
    }
}