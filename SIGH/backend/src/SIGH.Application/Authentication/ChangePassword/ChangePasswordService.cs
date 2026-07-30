using Microsoft.Extensions.Options;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Validators;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Authentication.ChangePassword;

public class ChangePasswordService : IChangePasswordService
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly PasswordOptions _passwordOptions;

    public ChangePasswordService(
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

    public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        // Validar complexidade da nova senha
        var (isValid, errorMessage) = PasswordComplexityValidator.Validate(request.NewPassword, _passwordOptions);
        if (!isValid)
        {
            throw new BusinessRuleValidationException(errorMessage);
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new BusinessRuleValidationException("Usuário não encontrado.");
        }

        // Validar senha atual
        bool isCurrentPasswordValid = _passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);
        if (!isCurrentPasswordValid)
        {
            throw new BusinessRuleValidationException("A senha atual informada está incorreta.");
        }

        // Verificar histórico das últimas N senhas (padrão 3)
        var recentHistories = await _userRepository.GetRecentPasswordHistoriesAsync(user.Id, _passwordOptions.PasswordHistoryLimit, cancellationToken);
        var recentPasswordHashes = recentHistories.Select(ph => ph.PasswordHash).ToList();

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

        if (user.Status == UserStatus.PendingFirstAccess)
        {
            user.Status = UserStatus.Active;
        }

        await _userRepository.AddPasswordHistoryAsync(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = newHash
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new ChangePasswordResponse(true, "Senha alterada com sucesso.");
    }
}
