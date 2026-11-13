using SIGH.Domain.Entities;
namespace SIGH.Application.Interfaces;

public interface IJwtTokenGenerator
{
    /// <param name="accessChangesAt">Próximo início/fim de vigência de perfil ou gerência (claim <c>acc_chg</c>).</param>
    string GenerateToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions, DateTimeOffset? accessChangesAt = null);
}
