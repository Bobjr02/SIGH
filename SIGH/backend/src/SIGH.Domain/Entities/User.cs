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

    /// <summary>
    /// Muda sempre que o acesso do usuário muda (desativação, bloqueio, senha, perfis, gerências).
    /// O access token carrega o valor da emissão; um token com stamp diferente do banco é recusado (401).
    /// </summary>
    public string SecurityStamp { get; set; } = NewSecurityStamp();

    // Navigation Properties
    public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public virtual ICollection<UserManagementUnit> ManagementUnits { get; set; } = new List<UserManagementUnit>();

    public User() : base() { }

    public User(Guid id) : base(id) { }

    public void RotateSecurityStamp() => SecurityStamp = NewSecurityStamp();

    public static string NewSecurityStamp() => Guid.NewGuid().ToString("N");
}
