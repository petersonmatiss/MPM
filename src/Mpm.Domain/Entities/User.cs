using System.ComponentModel.DataAnnotations;

namespace Mpm.Domain.Entities;

public class User : TenantEntity
{
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string Salt { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginDate { get; set; }

    public DateTime? PasswordChangedDate { get; set; }

    public int FailedLoginAttempts { get; set; } = 0;

    public DateTime? LockoutEndDate { get; set; }

    // Navigation properties
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

    // Computed properties
    public string FullName => $"{FirstName} {LastName}".Trim();

    public bool IsLockedOut => LockoutEndDate.HasValue && LockoutEndDate.Value > DateTime.UtcNow;
}

public class UserSession : TenantEntity
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string SessionToken { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime ExpiresDate { get; set; }

    public string? IPAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastActivityDate { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    // Computed properties
    public bool IsExpired => DateTime.UtcNow > ExpiresDate;
}