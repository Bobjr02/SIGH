using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Authentication.Logout;

public class LogoutService : ILogoutService
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LogoutService(
        IApplicationDbContext context,
        IUserRepository userRepository,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _userRepository = userRepository;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<LogoutResponse> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            var tokenHash = _tokenHasher.HashToken(request.RefreshToken);
            var token = await _userRepository.GetRefreshTokenByHashAsync(tokenHash, cancellationToken);

            if (token != null && token.UserId == request.UserId && token.RevokedAt == null)
            {
                token.RevokedAt = now;
                token.ReasonRevoked = "Logout efetuado pelo usuário";
            }
        }

        if (request.SessionId.HasValue)
        {
            var session = await _userRepository.GetUserSessionAsync(request.SessionId.Value, request.UserId, cancellationToken);

            if (session != null && !session.IsRevoked)
            {
                session.IsRevoked = true;
                session.RevokedAt = now;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new LogoutResponse(true, "Logout realizado com sucesso.");
    }
}
