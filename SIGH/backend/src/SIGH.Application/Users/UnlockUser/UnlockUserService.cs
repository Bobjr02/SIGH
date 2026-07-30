using SIGH.Application.Common.Interfaces;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Users.UnlockUser;

public class UnlockUserService : IUnlockUserService
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;

    public UnlockUserService(IApplicationDbContext context, IUserRepository userRepository)
    {
        _context = context;
        _userRepository = userRepository;
    }

    public async Task<UnlockUserResponse> UnlockUserAsync(UnlockUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new BusinessRuleValidationException("Usuário não encontrado.");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        if (user.Status == UserStatus.Locked)
        {
            user.Status = user.MustChangePassword ? UserStatus.PendingFirstAccess : UserStatus.Active;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UnlockUserResponse(true, "Usuário desbloqueado com sucesso.");
    }
}
