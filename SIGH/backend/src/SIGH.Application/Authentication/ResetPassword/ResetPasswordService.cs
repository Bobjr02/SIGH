using Microsoft.Extensions.Options;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Validators;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Authentication.ResetPassword;

public class ResetPasswordService : IResetPasswordService
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly PasswordOptions _passwordOptions;

    public ResetPasswordService(
        IApplicationDbContext context,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IOptions<PasswordOptions> passwordOptions)
    {
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _passwordOptions = passwordOptions.Value;
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // Validar complexidade da senha
        var (isValid, errorMessage) = PasswordComplexityValidator.Validate(request.NewPassword, _passwordOptions);
        if (!isValid)
        {
            throw new BusinessRuleValidationException(errorMessage);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user == null)
        {
            throw new BusinessRuleValidationException("Usuário não encontrado.");
        }

        // Verificar histórico das últimas N senhas (padrão 3)
        var recentHistories = await _userRepository.GetRecentPasswordHistoriesAsync(user.Id, _passwordOptions.PasswordHistoryLimit, cancellationToken);
        var recentPasswordHashes = recentHistories.Select(ph => ph.PasswordHash).ToList();

        // Incluir a senha atual no histórico de verificação
        recentPasswordHashes.Add(user.PasswordHash);

        foreach (var oldHash in recentPasswordHashes)
        {
            if (_passwordHasher.VerifyPassword(request.NewPassword, oldHash))
            {
                throw new BusinessRuleValidationException($"A nova senha não pode ser igual a nenhuma das últimas {_passwordOptions.PasswordHistoryLimit} senhas utilizadas.");
            }
        }

        var newHash = _passwordHasher.HashPassword(request.NewPassword);

        user.PasswordHash = newHash;
        user.MustChangePassword = false;
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        if (user.Status == UserStatus.PendingFirstAccess || user.Status == UserStatus.Locked)
        {
            user.Status = UserStatus.Active;
        }

        await _userRepository.AddPasswordHistoryAsync(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = newHash
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResponse(true, "Senha redefinida com sucesso.");
    }
}
