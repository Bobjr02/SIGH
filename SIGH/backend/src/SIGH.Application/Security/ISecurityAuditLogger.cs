namespace SIGH.Application.Security;

/// <summary>
/// Grava eventos de segurança na trilha de auditoria (AuditLogs). A gravação é imediata
/// (SaveChanges) para que o evento fique registrado mesmo que a operação termine em erro,
/// como um login com falha. Nunca passe senhas, tokens ou segredos em <c>details</c>.
/// </summary>
public interface ISecurityAuditLogger
{
    Task RecordAsync(
        string action,
        Guid? userId,
        object? details = null,
        CancellationToken cancellationToken = default,
        string? entityName = null,
        string? entityId = null);
}

/// <summary>Nomes dos eventos de segurança gravados em AuditLogs.Action.</summary>
public static class SecurityAuditActions
{
    public const string LoginSucceeded = "Security.LoginSucceeded";
    public const string LoginFailed = "Security.LoginFailed";
    public const string AccountLocked = "Security.AccountLocked";
    public const string PasswordChanged = "Security.PasswordChanged";
    public const string PasswordResetRequested = "Security.PasswordResetRequested";
    public const string PasswordReset = "Security.PasswordReset";
    public const string Logout = "Security.Logout";
    public const string RefreshTokenReuseDetected = "Security.RefreshTokenReuseDetected";
    public const string RolesOrPermissionsChanged = "Security.RolesOrPermissionsChanged";
    public const string EvidenceDownloaded = "Security.EvidenceDownloaded";
    public const string RoleAssigned = "Security.RoleAssigned";
    public const string RoleRevoked = "Security.RoleRevoked";
    public const string ManagementUnitAssigned = "Security.ManagementUnitAssigned";
    public const string ManagementUnitRevoked = "Security.ManagementUnitRevoked";
    public const string RefreshTokensRevoked = "Security.RefreshTokensRevoked";
}
