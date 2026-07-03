using BoardGameClubSoftware.Storage;
using Board_Game_Software.Data;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

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
builder.Services.AddHttpContextAccessor();

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

builder.Services.Configure<AzureBlobOptions>(
    builder.Configuration.GetSection("AzureBlob"));

builder.Services.Configure<ImageUploadValidationOptions>(
    builder.Configuration.GetSection("ImageUploads"));

builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
builder.Services.AddSingleton<IImageUploadValidator, ImageUploadValidator>();

// =========================
// Application Services
// =========================
// Added the RatingService for ELO calculations
builder.Services.AddScoped<RatingService>();
builder.Services.AddScoped<BoardGamePlayabilityService>();
builder.Services.AddScoped<AchievementService>();

builder.Services.AddScoped<GameNightService>();
builder.Services.AddScoped<ICurrentClubService, CurrentClubService>();
builder.Services.AddScoped<ImageService>();

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
    try
    {
        app.Logger.LogError(ex, "Startup seeding failed (DB/Identity). App will continue to start.");
    }
    catch
    {
        // Logging providers should not stop the app from booting after a recoverable startup seed failure.
    }
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
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionFeature?.Error;

            if (exception != null)
            {
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("UnhandledRequest");

                logger.LogError(exception, "Unhandled exception while processing {Path}", exceptionFeature?.Path);

                try
                {
                    var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                    var logDirectory = Path.Combine(environment.ContentRootPath, "logs");
                    Directory.CreateDirectory(logDirectory);

                    var logEntry = $"""
                        [{DateTimeOffset.UtcNow:O}] {exceptionFeature?.Path}
                        {exception}

                        """;

                    await File.AppendAllTextAsync(Path.Combine(logDirectory, "request-errors.log"), logEntry);
                }
                catch
                {
                    // Avoid a secondary logging failure hiding the original error page.
                }
            }

            context.Response.Redirect("/Error");
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (
    BoardGameDbContext db,
    IOptions<AzureBlobOptions> azureBlobOptions,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    var sqlConfigured = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection"));
    var sqlConnected = false;
    string? sqlError = null;

    if (sqlConfigured)
    {
        try
        {
            sqlConnected = await db.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            sqlError = ex.Message;
        }
    }

    var blobOptions = azureBlobOptions.Value;
    var blobConfigured =
        !string.IsNullOrWhiteSpace(blobOptions.ConnectionString) &&
        !string.IsNullOrWhiteSpace(blobOptions.ContainerName) &&
        !string.IsNullOrWhiteSpace(blobOptions.PublicBaseUrl);

    var healthy = sqlConfigured && sqlConnected && blobConfigured;
    var response = new
    {
        status = healthy ? "Healthy" : "Unhealthy",
        checks = new
        {
            sql = new
            {
                configured = sqlConfigured,
                connected = sqlConnected,
                error = sqlError
            },
            azureBlob = new
            {
                configured = blobConfigured,
                containerNameConfigured = !string.IsNullOrWhiteSpace(blobOptions.ContainerName),
                publicBaseUrlConfigured = !string.IsNullOrWhiteSpace(blobOptions.PublicBaseUrl)
            }
        }
    };

    return healthy
        ? Results.Ok(response)
        : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapRazorPages();
app.MapControllers();

app.Run();
