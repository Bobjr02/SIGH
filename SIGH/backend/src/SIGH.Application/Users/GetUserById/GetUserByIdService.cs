using SIGH.Domain.Repositories;

namespace SIGH.Application.Users.GetUserById;

public class GetUserByIdService : IGetUserByIdService
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDetailDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAndPermissionsAsync(id, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"Usuário com o ID {id} não foi encontrado.");
        }

        var roles = user.UserRoles
            .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var permissions = user.UserRoles
            .Where(ur => !ur.IsDeleted && ur.Role != null && !ur.Role.IsDeleted)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Where(rp => !rp.IsDeleted && rp.Permission != null && !rp.Permission.IsDeleted)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        return new UserDetailDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Cpf,
            user.Status.ToString(),
            user.MustChangePassword,
            user.FailedLoginAttempts,
            user.LockoutEnd,
            user.LastLoginAt,
            roles,
            permissions,
            user.CreatedAt,
            user.UpdatedAt
        );
    }
}
