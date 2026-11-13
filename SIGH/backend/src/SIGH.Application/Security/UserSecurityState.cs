using SIGH.Domain.Enums;

namespace SIGH.Application.Security;

/// <summary>O que a API precisa saber, a cada requisição, para aceitar o access token de um usuário.</summary>
public sealed record UserSecurityState(string SecurityStamp, UserStatus Status)
{
    public bool CanUseTheSystem => Status is not (UserStatus.Inactive or UserStatus.Suspended or UserStatus.Locked);
}

public interface IUserSecurityStateReader
{
    /// <returns>null se o usuário não existe (ou foi excluído).</returns>
    Task<UserSecurityState?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}
