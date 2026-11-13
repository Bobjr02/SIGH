using Microsoft.Extensions.Options;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Validators;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Users.CreateUser;

public class CreateUserService : ICreateUserService
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly PasswordOptions _passwordOptions;
    private readonly SIGH.Application.Authentication.PasswordTokens.IPasswordTokenService? _tokens;
    private readonly SIGH.Application.Email.IEmailOutbox? _outbox;
    private readonly IRandomStringGenerator? _random;
    private readonly AppOptions _appOptions;

    public CreateUserService(
        IApplicationDbContext context,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IOptions<PasswordOptions> passwordOptions,
        SIGH.Application.Authentication.PasswordTokens.IPasswordTokenService? tokens = null,
        SIGH.Application.Email.IEmailOutbox? outbox = null,
        IRandomStringGenerator? random = null,
        IOptions<AppOptions>? appOptions = null)
    {
        _tokens = tokens;
        _outbox = outbox;
        _random = random;
        _appOptions = appOptions?.Value ?? new AppOptions();
        _context = context;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _passwordOptions = passwordOptions.Value;
    }

    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        // Senha inicial opcional. Sem ela, a conta recebe uma senha aleatória que ninguém conhece:
        // o acesso só é possível pelo link de definição de senha enviado por e-mail.
        var hasInitialPassword = !string.IsNullOrWhiteSpace(request.InitialPassword);
        if (hasInitialPassword)
        {
            var (isValid, errorMessage) = PasswordComplexityValidator.Validate(request.InitialPassword!, _passwordOptions);
            if (!isValid)
            {
                throw new BusinessRuleValidationException(errorMessage);
            }
        }
        else if (_random is null)
        {
            throw new BusinessRuleValidationException("A senha inicial é obrigatória.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var cpf = request.Cpf.Trim();

        bool emailExists = await _userRepository.ExistsByEmailAsync(email, cancellationToken);
        if (emailExists)
        {
            throw new BusinessRuleValidationException("Já existe um usuário cadastrado com este e-mail corporativo.");
        }

        bool cpfExists = await _userRepository.ExistsByCpfAsync(cpf, cancellationToken);
        if (cpfExists)
        {
            throw new BusinessRuleValidationException("Já existe um usuário cadastrado com este CPF.");
        }

        // Verificar validade das Roles fornecidas
        var roles = await _userRepository.GetRolesByIdsAsync(request.RoleIds, cancellationToken);

        if (roles.Count != request.RoleIds.Distinct().Count())
        {
            throw new BusinessRuleValidationException("Um ou mais perfis (Roles) fornecidos são inválidos ou inexistentes.");
        }

        if (roles.Any(r => Common.Authorization.SystemRoles.Find(r.Name) is null))
        {
            throw new BusinessRuleValidationException("Só os perfis de sistema podem ser atribuídos.");
        }

        var managementUnitIds = (request.ManagementUnitIds ?? []).Distinct().ToList();
        if (roles.Any(r => r.Name == Common.Authorization.SystemRoles.Manager) && managementUnitIds.Count == 0)
        {
            throw new BusinessRuleValidationException(Access.UserAccessRules.ManagerWithoutUnitMessage);
        }

        if (managementUnitIds.Count > 0)
        {
            var found = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
                _context.ManagementUnits, m => managementUnitIds.Contains(m.Id), cancellationToken);
            if (found != managementUnitIds.Count)
                throw new BusinessRuleValidationException("Uma ou mais gerências informadas não existem.");
        }

        var passwordHash = _passwordHasher.HashPassword(hasInitialPassword ? request.InitialPassword! : _random!.GenerateRandomString(64));

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Cpf = cpf,
            PasswordHash = passwordHash,
            Status = UserStatus.PendingFirstAccess,
            MustChangePassword = true,
            FailedLoginAttempts = 0
        };

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        foreach (var managementUnitId in managementUnitIds)
        {
            user.ManagementUnits.Add(new UserManagementUnit { UserId = user.Id, ManagementUnitId = managementUnitId });
        }

        user.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = passwordHash
        });

        await _userRepository.AddAsync(user, cancellationToken);

        // E-mail de conta criada, com link de definição de senha (nunca a senha). Mesma transação.
        if (_tokens is not null && _outbox is not null)
        {
            var rawToken = await _tokens.IssueAsync(user.Id, PasswordTokenPurpose.AccountSetup, cancellationToken);
            var hours = (int)_tokens.LifetimeOf(PasswordTokenPurpose.AccountSetup).TotalHours;
            _outbox.Enqueue(user.Email, SIGH.Application.Email.EmailTemplates.AccountCreated(
                user.FullName,
                user.Email,
                _appOptions.BuildUrl($"/reset-password?token={Uri.EscapeDataString(rawToken)}&setup=1"),
                hours,
                _appOptions.BuildUrl("/login")));
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateUserResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Cpf,
            user.Status.ToString(),
            user.MustChangePassword
        );
    }
}
