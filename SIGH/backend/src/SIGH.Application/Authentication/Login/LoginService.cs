using Microsoft.Extensions.Options;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;
using RefreshTokenEntity = SIGH.Domain.Entities.RefreshToken;

namespace SIGH.Application.Authentication.Login;

public class LoginService : ILoginService
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtOptions _jwtOptions;
    private readonly PasswordOptions _passwordOptions;
    private readonly TokenOptions _tokenOptions;

    public LoginService(
        IApplicationDbContext context,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IOptions<JwtOptions> jwtOptions,
        IOptions<PasswordOptions> passwordOptions,
        IOptions<TokenOptions> tokenOptions)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _jwtOptions = jwtOptions.Value;
        _passwordOptions = passwordOptions.Value;
        _tokenOptions = tokenOptions.Value;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailWithRolesAndPermissionsAsync(email, cancellationToken);

        if (user == null)
        {
            throw new BusinessRuleValidationException("E-mail ou senha inválidos.");
        }

        var now = _dateTimeProvider.UtcNow;

        // Verificar Bloqueio Ativo
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > now)
        {
            throw new BusinessRuleValidationException("Conta temporariamente bloqueada devido a múltiplas tentativas incorretas. Tente novamente mais tarde.");
        }

        if (user.Status == UserStatus.Locked)
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value <= now)
            {
                // Tempo de bloqueio expirou: desbloqueia automaticamente
                user.Status = user.MustChangePassword ? UserStatus.PendingFirstAccess : UserStatus.Active;
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
            }
            else
            {
                throw new BusinessRuleValidationException("Usuário bloqueado. Entre em contato com o administrador do sistema.");
            }
        }

        if (user.Status == UserStatus.Inactive || user.Status == UserStatus.Suspended)
        {
            throw new BusinessRuleValidationException("Usuário inativo ou suspenso. Acesso negado.");
        }

        // Validar Senha
        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= _passwordOptions.MaxFailedAccessAttempts)
            {
                user.Status = UserStatus.Locked;
                user.LockoutEnd = now.AddMinutes(_passwordOptions.LockoutDurationInMinutes);
            }

            await _context.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleValidationException("E-mail ou senha inválidos.");
        }

        // Sucesso no login: resetar falhas e atualizar login
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = now;

        if (user.Status == UserStatus.Locked)
        {
            user.Status = user.MustChangePassword ? UserStatus.PendingFirstAccess : UserStatus.Active;
        }

        // Extrair Roles e Permissões
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

        // Gerar Access Token e Refresh Token
        var accessToken = _jwtTokenGenerator.GenerateToken(user, roles, permissions);
        var rawRefreshToken = _refreshTokenGenerator.GenerateRefreshToken();
        var tokenHash = _tokenHasher.HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshTokenEntity
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = now.AddDays(_tokenOptions.RefreshTokenExpirationInDays),
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        };

        await _userRepository.AddRefreshTokenAsync(refreshTokenEntity, cancellationToken);

        // Criar Sessão do Usuário
        var sessionEntity = new UserSession
        {
            UserId = user.Id,
            DeviceName = request.DeviceName,
            Browser = request.Browser,
            OperatingSystem = request.OperatingSystem,
            IpAddress = request.IpAddress,
            StartedAt = now,
            LastActivity = now,
            IsRevoked = false
        };

        await _userRepository.AddUserSessionAsync(sessionEntity, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Cpf,
            user.Status.ToString(),
            user.MustChangePassword,
            roles,
            permissions
        );

        return new LoginResponse(
            accessToken,
            rawRefreshToken,
            _jwtOptions.ExpiryInMinutes > 0 ? _jwtOptions.ExpiryInMinutes : 60,
            user.MustChangePassword,
            userDto
        );
    }
}
