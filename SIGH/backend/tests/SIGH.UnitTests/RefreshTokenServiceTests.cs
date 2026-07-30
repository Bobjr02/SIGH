using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using SIGH.Application.Authentication.RefreshToken;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Infrastructure.Authentication;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.UnitTests;

public class RefreshTokenServiceTests
{
    private SighDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SighDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SighDbContext(options);
    }

    private Sha256TokenHasher _tokenHasher = new();
    private RefreshTokenGenerator _refreshTokenGenerator = new();

    [Fact]
    public async Task RefreshTokenAsync_WithValidActiveToken_ShouldRotateToken()
    {
        using var context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Inspetor Souza",
            Email = "inspetor.souza@policiacivil.sp.gov.br",
            Cpf = "44455566677",
            PasswordHash = "hash",
            Status = UserStatus.Active
        };
        await context.Users.AddAsync(user);

        var rawToken = "initial-refresh-token-123456789";
        var tokenHash = _tokenHasher.HashToken(rawToken);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        await context.RefreshTokens.AddAsync(refreshToken);
        await context.SaveChangesAsync();

        var mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        mockJwtGenerator.Setup(j => j.GenerateToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns("new-jwt-access-token");

        var mockDateTime = new Mock<IDateTimeProvider>();
        mockDateTime.Setup(d => d.UtcNow).Returns(DateTimeOffset.UtcNow);

        var service = new RefreshTokenService(
            context,
            mockJwtGenerator.Object,
            _refreshTokenGenerator,
            _tokenHasher,
            mockDateTime.Object,
            Options.Create(new JwtOptions { ExpiryInMinutes = 60 }),
            Options.Create(new TokenOptions { RefreshTokenExpirationInDays = 7 })
        );

        var request = new RefreshTokenRequest(rawToken);
        var response = await service.RefreshTokenAsync(request);

        response.Should().NotBeNull();
        response.AccessToken.Should().Be("new-jwt-access-token");
        response.RefreshToken.Should().NotBeNullOrWhiteSpace();
        response.RefreshToken.Should().NotBe(rawToken);

        var oldTokenInDb = await context.RefreshTokens.FirstAsync(rt => rt.TokenHash == tokenHash);
        oldTokenInDb.RevokedAt.Should().NotBeNull();
        oldTokenInDb.ReasonRevoked.Should().Be("Rotacionado");

        var newTokensCount = await context.RefreshTokens.CountAsync(rt => rt.UserId == user.Id && rt.RevokedAt == null);
        newTokensCount.Should().Be(1);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithReusedToken_ShouldRevokeAllUserTokensAndSessions()
    {
        using var context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Perito Lima",
            Email = "perito.lima@policiacivil.sp.gov.br",
            Cpf = "55566677788",
            PasswordHash = "hash",
            Status = UserStatus.Active
        };
        await context.Users.AddAsync(user);

        var reusedRawToken = "reused-token-xyz";
        var reusedHash = _tokenHasher.HashToken(reusedRawToken);

        // Token já revogado anteriormente
        var revokedToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = reusedHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            ReasonRevoked = "Rotacionado"
        };

        // Outro token ativo do usuário
        var activeToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenHasher.HashToken("another-active-token"),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        // Sessão ativa do usuário
        var activeSession = new UserSession
        {
            UserId = user.Id,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            IsRevoked = false
        };

        await context.RefreshTokens.AddRangeAsync(revokedToken, activeToken);
        await context.UserSessions.AddAsync(activeSession);
        await context.SaveChangesAsync();

        var mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        var mockDateTime = new Mock<IDateTimeProvider>();
        mockDateTime.Setup(d => d.UtcNow).Returns(DateTimeOffset.UtcNow);

        var service = new RefreshTokenService(
            context,
            mockJwtGenerator.Object,
            _refreshTokenGenerator,
            _tokenHasher,
            mockDateTime.Object,
            Options.Create(new JwtOptions()),
            Options.Create(new TokenOptions())
        );

        var request = new RefreshTokenRequest(reusedRawToken);

        var act = async () => await service.RefreshTokenAsync(request);

        await act.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*Tentativa de reuso de token detectada*");

        var activeTokensInDb = await context.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.RevokedAt == null).ToListAsync();
        activeTokensInDb.Should().BeEmpty();

        var dbSession = await context.UserSessions.FindAsync(activeSession.Id);
        dbSession!.IsRevoked.Should().BeTrue();
    }
}
