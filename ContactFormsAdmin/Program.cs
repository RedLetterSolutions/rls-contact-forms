using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ContactFormsAdmin.Data;
using ContactFormsAdmin.Services;
using ContactFormsAdmin.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Services
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddHttpClient();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    
    // Seed initial data
    await SiteSeeder.SeedSitesAsync(db);

    // Seed initial admin user if none exist
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var adminService = scope.ServiceProvider.GetRequiredService<AdminUserService>();
    // Ensure admin_users table exists (idempotent)
    await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS admin_users (
        id BIGSERIAL PRIMARY KEY,
        username VARCHAR(100) NOT NULL UNIQUE,
        password_hash TEXT NOT NULL,
        is_active BOOLEAN NOT NULL DEFAULT TRUE,
        created_at TIMESTAMPTZ NOT NULL
    );");
    await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_admin_users_username ON admin_users (username);");

    if (!await db.AdminUsers.AnyAsync())
    {
        var initialUser = config["AdminUsername"] ?? "admin";
        var initialPass = config["AdminPassword"] ?? Guid.NewGuid().ToString("n").Substring(0, 12);
        await adminService.CreateAsync(initialUser, initialPass);
        app.Logger.LogInformation("Seeded default admin user '{User}'.", initialUser);
        app.Logger.LogWarning("Default admin password created. Please change it immediately. Username: {User} Password: {Pass}", initialUser, initialPass);
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// API Key middleware (for /api routes)
app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
