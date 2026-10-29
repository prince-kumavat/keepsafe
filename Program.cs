using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using KeepSafe.Infrastructure;
using KeepSafe.Models;
using KeepSafe.Services;

var builder = WebApplication.CreateBuilder(args);

// Kestrel upload limits (10 MB)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

// Configure Database Connection String
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var dbHost = builder.Configuration["MYSQL_HOST"] ?? "db";
    var dbPort = builder.Configuration["MYSQL_PORT"] ?? "3306";
    var dbDatabase = builder.Configuration["MYSQL_DATABASE"] ?? "keepsafe";
    var dbUser = builder.Configuration["MYSQL_USER"] ?? "keepsafe";
    var dbPass = builder.Configuration["MYSQL_PASSWORD"] ?? "";

    connectionString = $"Server={dbHost};Port={dbPort};Database={dbDatabase};User={dbUser};Password={dbPass};TreatTinyAsBoolean=true;";
}

var serverVersion = new MySqlServerVersion(new Version(8, 4, 0));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        serverVersion,
        mySqlOptions =>
        {
            mySqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
        });
});

// Data Protection keys persistence in /keys
var keysDirectory = new DirectoryInfo("/keys");
if (!keysDirectory.Exists)
{
    try
    {
        keysDirectory.Create();
    }
    catch
    {
        // In local non-docker development fallback to local directory
        keysDirectory = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys"));
        if (!keysDirectory.Exists) keysDirectory.Create();
    }
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(keysDirectory)
    .SetApplicationName("KeepSafe");

// Form options upload limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "KeepSafe.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/";
        options.LogoutPath = "/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// Register Application Services
builder.Services.AddSingleton<IExpiryClock, ExpiryClock>();
builder.Services.AddSingleton<IDocumentProcessor, DocumentProcessor>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWarrantyService, WarrantyService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Database Migration & Initialization with Retries
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    int retries = 10;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Applying database migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning("Database initialization attempt failed ({RetriesLeft} attempts left): {Message}", retries, ex.Message);
            if (retries == 0)
            {
                logger.LogError(ex, "Failed to connect to the database after multiple attempts.");
                throw;
            }
            Thread.Sleep(3000);
        }
    }
}

// Forwarded headers for reverse proxies / Docker
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
