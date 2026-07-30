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

    public CreateUserService(
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

    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        // Validar complexidade da senha inicial
        var (isValid, errorMessage) = PasswordComplexityValidator.Validate(request.InitialPassword, _passwordOptions);
        if (!isValid)
        {
            throw new BusinessRuleValidationException(errorMessage);
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

        var passwordHash = _passwordHasher.HashPassword(request.InitialPassword);

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

        user.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = passwordHash
        });

        await _userRepository.AddAsync(user, cancellationToken);
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
