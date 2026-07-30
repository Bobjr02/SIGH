using FluentAssertions;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using Xunit;

namespace SIGH.UnitTests;

public class AuthDomainModelTests
{
    [Fact]
    public void User_ShouldInitializeWithDefaultValues()
    {
        var user = new User
        {
            FullName = "João da Silva",
            Email = "joao@sigh.com.br",
            Cpf = "12345678901",
            PasswordHash = "hashedpassword"
        };

        user.Status.Should().Be(UserStatus.PendingFirstAccess);
        user.MustChangePassword.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        user.PasswordHistories.Should().NotBeNull();
        user.RefreshTokens.Should().NotBeNull();
        user.UserRoles.Should().NotBeNull();
        user.Sessions.Should().NotBeNull();
    }

    [Fact]
    public void RoleAndPermission_ShouldEstablishRelationships()
    {
        var adminRole = new Role { Name = "Administrator", Description = "Acesso total" };
        var createPermission = new Permission { Code = "Users.Create", Name = "Criar Usuários" };

        var rolePermission = new RolePermission
        {
            Role = adminRole,
            RoleId = adminRole.Id,
            Permission = createPermission,
            PermissionId = createPermission.Id
        };

        adminRole.RolePermissions.Add(rolePermission);

        adminRole.RolePermissions.Should().Contain(rolePermission);
        rolePermission.Permission.Code.Should().Be("Users.Create");
    }

    [Fact]
    public void RefreshToken_ShouldStoreTokenHashAndRevocation()
    {
        var refreshToken = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "abcdef123456hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IpAddress = "127.0.0.1",
            UserAgent = "Mozilla/5.0"
        };

        refreshToken.TokenHash.Should().Be("abcdef123456hash");
        refreshToken.RevokedAt.Should().BeNull();

        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        refreshToken.ReasonRevoked = "User Logout";

        refreshToken.RevokedAt.Should().NotBeNull();
        refreshToken.ReasonRevoked.Should().Be("User Logout");
    }

    [Fact]
    public void UserSession_ShouldTrackActivityAndRevocation()
    {
        var session = new UserSession
        {
            UserId = Guid.NewGuid(),
            DeviceName = "Chrome Desktop",
            Browser = "Chrome",
            OperatingSystem = "Windows",
            IpAddress = "192.168.1.1"
        };

        session.IsRevoked.Should().BeFalse();
        session.StartedAt.Should().NotBe(default);

        session.IsRevoked = true;
        session.RevokedAt = DateTimeOffset.UtcNow;

        session.IsRevoked.Should().BeTrue();
    }
}
