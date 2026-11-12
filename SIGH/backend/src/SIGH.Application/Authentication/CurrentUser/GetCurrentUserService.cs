using SIGH.Application.Authentication.Common;
using SIGH.Domain.Enums;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Authentication.CurrentUser;

public class GetCurrentUserService : IGetCurrentUserService
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<CurrentUserResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAndPermissionsAsync(userId, cancellationToken);

        // Usuário removido ou desativado depois da emissão do token: a sessão não vale mais.
        if (user is null || user.Status is UserStatus.Inactive or UserStatus.Suspended)
        {
            throw new UnauthorizedAccessException("Sessão inválida: usuário não encontrado ou inativo.");
        }

        return new CurrentUserResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Status.ToString(),
            user.MustChangePassword,
            user.GetActiveRoleNames(),
            user.GetActivePermissionCodes());
    }
}
