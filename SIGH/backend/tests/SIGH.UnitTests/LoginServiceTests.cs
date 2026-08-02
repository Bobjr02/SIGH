using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using SIGH.Application.Authentication.Login;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;
using SIGH.Infrastructure.Authentication;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.UnitTests;

public class LoginServiceTests
{
    private SighDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SighDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SighDbContext(options);
    }

    private BCryptPasswordHasher _passwordHasher = new();
    private Sha256TokenHasher _tokenHasher = new();
    private RefreshTokenGenerator _refreshTokenGenerator = new();

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponse()
    {
        using var context = CreateDbContext();
        var rawPassword = "Password123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Delegado Silva",
            Email = "delegado.silva@policiacivil.sp.gov.br",
            Cpf = "11122233344",
            PasswordHash = _passwordHasher.HashPassword(rawPassword),
            Status = UserStatus.Active,
            MustChangePassword = false
        };

        var role = new Role { Id = Guid.NewGuid(), Name = "Delegado", Code = "ROLE_DELEGADO", Description = "Delegado" };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role });

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        mockJwtGenerator.Setup(j => j.GenerateToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns("fake-jwt-access-token");

        var mockDateTime = new Mock<IDateTimeProvider>();
        mockDateTime.Setup(d => d.UtcNow).Returns(DateTimeOffset.UtcNow);
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailWithRolesAndPermissionsAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepositoryMock
            .Setup(r => r.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => context.RefreshTokens.Add(token))
            .Returns(Task.CompletedTask);
        userRepositoryMock
            .Setup(r => r.AddUserSessionAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()))
            .Callback<UserSession, CancellationToken>((session, _) => context.UserSessions.Add(session))
            .Returns(Task.CompletedTask);

        var loginService = new LoginService(
            context,
            userRepositoryMock.Object,
            _passwordHasher,
            mockJwtGenerator.Object,
            _refreshTokenGenerator,
            _tokenHasher,
            mockDateTime.Object,
            Options.Create(new JwtOptions { ExpiryInMinutes = 60 }),
            Options.Create(new PasswordOptions { MaxFailedAccessAttempts = 5 }),
            Options.Create(new TokenOptions { RefreshTokenExpirationInDays = 7 })
        );

        var request = new LoginRequest("delegado.silva@policiacivil.sp.gov.br", rawPassword);

        var result = await loginService.LoginAsync(request);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("fake-jwt-access-token");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be("delegado.silva@policiacivil.sp.gov.br");

        var dbUser = await context.Users.FindAsync(user.Id);
        dbUser!.FailedLoginAttempts.Should().Be(0);
        dbUser.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldIncrementFailedAttempts()
    {
        using var context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Agente Santos",
            Email = "agente.santos@policiacivil.sp.gov.br",
            Cpf = "22233344455",
            PasswordHash = _passwordHasher.HashPassword("CorrectPassword123!"),
            Status = UserStatus.Active,
            FailedLoginAttempts = 0
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        var mockDateTime = new Mock<IDateTimeProvider>();
        mockDateTime.Setup(d => d.UtcNow).Returns(DateTimeOffset.UtcNow);
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailWithRolesAndPermissionsAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var loginService = new LoginService(
            context,
            userRepositoryMock.Object,
            _passwordHasher,
            mockJwtGenerator.Object,
            _refreshTokenGenerator,
            _tokenHasher,
            mockDateTime.Object,
            Options.Create(new JwtOptions()),
            Options.Create(new PasswordOptions { MaxFailedAccessAttempts = 5, LockoutDurationInMinutes = 15 }),
            Options.Create(new TokenOptions())
        );

        var request = new LoginRequest("agente.santos@policiacivil.sp.gov.br", "WrongPassword123!");

        var act = async () => await loginService.LoginAsync(request);

        await act.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*E-mail ou senha inválidos.*");

        var dbUser = await context.Users.FindAsync(user.Id);
        dbUser!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_WhenExceedingMaxFailedAttempts_ShouldLockAccount()
    {
        using var context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Escrivao Oliveira",
            Email = "escrivao.oliveira@policiacivil.sp.gov.br",
            Cpf = "33344455566",
            PasswordHash = _passwordHasher.HashPassword("CorrectPassword123!"),
            Status = UserStatus.Active,
            FailedLoginAttempts = 4
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        var mockDateTime = new Mock<IDateTimeProvider>();
        mockDateTime.Setup(d => d.UtcNow).Returns(DateTimeOffset.UtcNow);
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByEmailWithRolesAndPermissionsAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var loginService = new LoginService(
            context,
            userRepositoryMock.Object,
            _passwordHasher,
            mockJwtGenerator.Object,
            _refreshTokenGenerator,
            _tokenHasher,
            mockDateTime.Object,
            Options.Create(new JwtOptions()),
            Options.Create(new PasswordOptions { MaxFailedAccessAttempts = 5, LockoutDurationInMinutes = 15 }),
            Options.Create(new TokenOptions())
        );

        var request = new LoginRequest("escrivao.oliveira@policiacivil.sp.gov.br", "WrongPassword123!");

        var act = async () => await loginService.LoginAsync(request);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();

        var dbUser = await context.Users.FindAsync(user.Id);
        dbUser!.FailedLoginAttempts.Should().Be(5);
        dbUser.Status.Should().Be(UserStatus.Locked);
        dbUser.LockoutEnd.Should().NotBeNull();
    }
}
