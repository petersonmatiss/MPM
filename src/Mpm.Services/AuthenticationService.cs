using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Mpm.Data;
using Mpm.Domain.Entities;

namespace Mpm.Services;

public interface IAuthenticationService
{
    Task<(bool Success, User? User, string? ErrorMessage)> AuthenticateAsync(string username, string password);
    Task<User> CreateUserAsync(string username, string email, string firstName, string lastName, string password);
    Task<bool> ValidateSessionAsync(string sessionToken);
    Task<UserSession?> CreateSessionAsync(int userId, string? ipAddress = null, string? userAgent = null);
    Task<bool> InvalidateSessionAsync(string sessionToken);
    Task<User?> GetUserBySessionTokenAsync(string sessionToken);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<bool> ResetPasswordAsync(string email, string newPassword);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly MpmDbContext _context;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SessionDuration = TimeSpan.FromHours(8);

    public AuthenticationService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, User? User, string? ErrorMessage)> AuthenticateAsync(string username, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

        if (user == null)
        {
            return (false, null, "Invalid username or password.");
        }

        if (!user.IsActive)
        {
            return (false, null, "Account is disabled.");
        }

        if (user.IsLockedOut)
        {
            return (false, null, $"Account is locked until {user.LockoutEndDate:yyyy-MM-dd HH:mm} UTC.");
        }

        if (!VerifyPassword(password, user.PasswordHash, user.Salt))
        {
            // Increment failed attempts
            user.FailedLoginAttempts++;
            
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEndDate = DateTime.UtcNow.Add(LockoutDuration);
            }

            await _context.SaveChangesAsync();
            return (false, null, "Invalid username or password.");
        }

        // Reset failed attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LockoutEndDate = null;
        user.LastLoginDate = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return (true, user, null);
    }

    public async Task<User> CreateUserAsync(string username, string email, string firstName, string lastName, string password)
    {
        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);

        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this username or email already exists.");
        }

        var salt = GenerateSalt();
        var passwordHash = HashPassword(password, salt);

        var user = new User
        {
            Username = username,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = passwordHash,
            Salt = salt,
            IsActive = true,
            PasswordChangedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> ValidateSessionAsync(string sessionToken)
    {
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

        if (session == null || session.IsExpired || !session.User.IsActive)
        {
            return false;
        }

        // Update last activity
        session.LastActivityDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<UserSession?> CreateSessionAsync(int userId, string? ipAddress = null, string? userAgent = null)
    {
        var sessionToken = GenerateSessionToken();

        var session = new UserSession
        {
            UserId = userId,
            SessionToken = sessionToken,
            CreatedDate = DateTime.UtcNow,
            ExpiresDate = DateTime.UtcNow.Add(SessionDuration),
            IPAddress = ipAddress,
            UserAgent = userAgent,
            IsActive = true,
            LastActivityDate = DateTime.UtcNow
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<bool> InvalidateSessionAsync(string sessionToken)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        if (session == null)
        {
            return false;
        }

        session.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<User?> GetUserBySessionTokenAsync(string sessionToken)
    {
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

        if (session == null || session.IsExpired || !session.User.IsActive)
        {
            return null;
        }

        return session.User;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        if (!VerifyPassword(currentPassword, user.PasswordHash, user.Salt))
        {
            return false;
        }

        var newSalt = GenerateSalt();
        user.PasswordHash = HashPassword(newPassword, newSalt);
        user.Salt = newSalt;
        user.PasswordChangedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return false;
        }

        var newSalt = GenerateSalt();
        user.PasswordHash = HashPassword(newPassword, newSalt);
        user.Salt = newSalt;
        user.PasswordChangedDate = DateTime.UtcNow;
        user.FailedLoginAttempts = 0;
        user.LockoutEndDate = null;

        await _context.SaveChangesAsync();
        return true;
    }

    private static string GenerateSalt()
    {
        var salt = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return Convert.ToBase64String(salt);
    }

    private static string HashPassword(string password, string salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 10000, HashAlgorithmName.SHA256))
        {
            var hash = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hash);
        }
    }

    private static bool VerifyPassword(string password, string hash, string salt)
    {
        var computedHash = HashPassword(password, salt);
        return computedHash == hash;
    }

    private static string GenerateSessionToken()
    {
        var tokenBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        return Convert.ToBase64String(tokenBytes);
    }
}