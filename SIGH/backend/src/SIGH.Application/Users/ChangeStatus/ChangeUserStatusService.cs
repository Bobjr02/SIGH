using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Users.ChangeStatus;

public class ChangeUserStatusService : IChangeUserStatusService
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeUserStatusService(
        IApplicationDbContext context,
        IUserRepository userRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ChangeUserStatusResponse> ChangeUserStatusAsync(ChangeUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new BusinessRuleValidationException("Usuário não encontrado.");
        }

        user.Status = request.NewStatus;

        // Se o novo status inativa/bloqueia o usuário, revoga os tokens e sessões ativas
        if (request.NewStatus == UserStatus.Inactive ||
            request.NewStatus == UserStatus.Locked ||
            request.NewStatus == UserStatus.Suspended)
        {
            var now = _dateTimeProvider.UtcNow;

            var activeTokens = await _userRepository.GetActiveRefreshTokensByUserIdAsync(user.Id, cancellationToken);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;
                token.ReasonRevoked = $"Alteração de status do usuário para {request.NewStatus}: {request.Reason}";
            }

            var activeSessions = await _userRepository.GetActiveUserSessionsByUserIdAsync(user.Id, cancellationToken);

            foreach (var session in activeSessions)
            {
                session.IsRevoked = true;
                session.RevokedAt = now;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ChangeUserStatusResponse(true, $"Status do usuário alterado com sucesso para {request.NewStatus}.");
    }
}
