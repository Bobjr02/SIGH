namespace SIGH.Application.Common.Authorization;

/// <summary>
/// Claims próprias do SIGH emitidas no access token.
/// </summary>
public static class SighClaimTypes
{
    /// <summary>
    /// Presente (com valor "true") enquanto o usuário precisa trocar a senha.
    /// Com ela, o token só dá acesso a change-password, logout e refresh.
    /// </summary>
    public const string PasswordChangeRequired = "pwd_change_required";
}
