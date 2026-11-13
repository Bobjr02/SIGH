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

    /// <summary>SecurityStamp do usuário na emissão. Diferente do banco → 401.</summary>
    public const string SecurityStamp = "sstamp";

    /// <summary>
    /// Próximo instante (Unix, segundos) em que um perfil ou gerência começa ou termina.
    /// Passado esse instante → 401, e o front renova com as permissões recalculadas.
    /// </summary>
    public const string AccessChangesAt = "acc_chg";
}
