using System.Text.RegularExpressions;
using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Board_Game_Software.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

var resolvedSql = builder.Configuration.GetConnectionString("DefaultConnection");

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/DataSetup");
    options.Conventions.AuthorizeFolder("/DataSetup/Markers", "AdminOnly");
    options.Conventions.AuthorizeFolder("/DataSetup/Publishers", "AdminOnly");
    options.Conventions.AuthorizeFolder("/DataSetup/Shelves", "AdminOnly");
    options.Conventions.AuthorizeFolder("/GameNight");
    options.Conventions.AuthorizeFolder("/Match");
    options.Conventions.AuthorizePage("/Browsing/BoardGames/Add", "AdminOnly");
    options.Conventions.AuthorizePage("/Browsing/BoardGames/Edit", "AdminOnly");
});
builder.Services.AddControllers();

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

// =========================
// Application Services
// =========================
builder.Services.AddSingleton<BoardGameImagesService>();

// Added the RatingService for ELO calculations
builder.Services.AddScoped<RatingService>();
builder.Services.AddScoped<BoardGamePlayabilityService>();
builder.Services.AddScoped<AchievementService>();

builder.Services.AddScoped<GameNightService>();

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

    var adminEmail = app.Configuration["Identity:BootstrapAdminEmail"];
    if (!string.IsNullOrWhiteSpace(adminEmail))
    {
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    app.Logger.LogInformation("Startup role seeding completed successfully.");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Startup seeding failed (DB/Identity). App will continue to start.");
}

// =========================
// Middleware
// =========================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();

app.Run();
