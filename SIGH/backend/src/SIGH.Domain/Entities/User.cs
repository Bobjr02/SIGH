namespace SIGH.Domain.Entities;

public class User : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.PendingFirstAccess;
    public bool MustChangePassword { get; set; } = true;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    // Navigation Properties
    public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();

    public User() : base() { }

    public User(Guid id) : base(id) { }
}
