using System.Text.RegularExpressions;
using Board_Game_Software.Data;
using Board_Game_Software.Models; // Assuming BoardGameDbContext is here
using Board_Game_Software.Services;
using Board_Game_Software.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// =========================
// DEBUG CONFIG DUMP (safe)
// =========================
static string MaskSqlPassword(string conn)
{
    if (string.IsNullOrWhiteSpace(conn)) return conn;

    // Mask Password=...;
    conn = Regex.Replace(conn, @"(?i)(Password\s*=\s*)([^;]*)(;|$)", "$1***$3");

    // Also mask Pwd=...;
    conn = Regex.Replace(conn, @"(?i)(Pwd\s*=\s*)([^;]*)(;|$)", "$1***$3");

    return conn;
}

// Log which environment we think we are
Console.WriteLine($"DEBUG: ASPNETCORE_ENVIRONMENT = {builder.Environment.EnvironmentName}");

// Log SQL connection string that config resolves (password masked)
var resolvedSql = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(resolvedSql))
{
    Console.WriteLine("DEBUG: SQL DefaultConnection is NULL/EMPTY");
}
else
{
    Console.WriteLine("DEBUG: SQL DefaultConnection = " + MaskSqlPassword(resolvedSql));
}

// Log Mongo settings presence (do not dump full connection string)
var resolvedMongoConn = builder.Configuration["MongoDbSettings:ConnectionString"];
var resolvedMongoDb = builder.Configuration["MongoDbSettings:Database"];

Console.WriteLine(string.IsNullOrWhiteSpace(resolvedMongoConn)
    ? "DEBUG: MongoDbSettings:ConnectionString is NULL/EMPTY"
    : "DEBUG: MongoDbSettings:ConnectionString is PRESENT");

Console.WriteLine($"DEBUG: MongoDbSettings:Database = {(string.IsNullOrWhiteSpace(resolvedMongoDb) ? "(NULL/EMPTY)" : resolvedMongoDb)}");

// =========================
// SQL Database & Identity
// =========================
var connectionString = resolvedSql
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContextMain>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<BoardGameDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContextMain>();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>,
    UserClaimsPrincipalFactory<IdentityUser, IdentityRole>>();

// Razor Pages
builder.Services.AddRazorPages();

// =========================
// MongoDB Setup
// =========================
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = builder.Configuration
        .GetSection("MongoDbSettings")
        .Get<MongoDbSettings>();

    if (settings == null)
        throw new InvalidOperationException("MongoDbSettings section is missing or invalid.");

    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddSingleton<BoardGameImagesService>();

var app = builder.Build();

// =========================
// Startup seeding (safe)
// =========================
try
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string[] roleNames = { "Admin", "Moderator", "User" };

    foreach (var role in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = "mike_j_spencer@sky.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    Console.WriteLine("DEBUG: Startup seeding completed successfully.");
}
catch (Exception ex)
{
    // IMPORTANT: don’t crash the entire app if DB is unreachable on startup
    app.Logger.LogError(ex, "Startup seeding failed (DB/Identity). App will continue to start.");
    Console.WriteLine("DEBUG: Startup seeding failed: " + ex.Message);
}

// =========================
// Middleware
// =========================
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
