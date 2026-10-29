using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KeepSafe.Models;

namespace KeepSafe.Services;

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }
}

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, HttpContext httpContext);
    Task<AuthResult> LoginAsync(string email, string password, HttpContext httpContext);
    Task LogoutAsync(HttpContext httpContext);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, HttpContext httpContext)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // Derive display name from email's local part
        var atIndex = normalizedEmail.IndexOf('@');
        var displayName = atIndex > 0 ? normalizedEmail.Substring(0, atIndex) : normalizedEmail;
        if (displayName.Length > 100) displayName = displayName.Substring(0, 100);

        var existingUser = await _dbContext.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (existingUser)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An account with this email already exists."
            };
        }

        var user = new User
        {
            Email = normalizedEmail,
            Name = displayName,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        try
        {
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Handle race condition on duplicate email
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An account with this email already exists."
            };
        }

        await SignInUserAsync(user, httpContext);

        return new AuthResult
        {
            Success = true,
            User = user
        };
    }

    public async Task<AuthResult> LoginAsync(string email, string password, HttpContext httpContext)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        if (user == null)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Invalid email or password."
            };
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Invalid email or password."
            };
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _dbContext.SaveChangesAsync();
        }

        await SignInUserAsync(user, httpContext);

        return new AuthResult
        {
            Success = true,
            User = user
        };
    }

    public async Task LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static async Task SignInUserAsync(User user, HttpContext httpContext)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
