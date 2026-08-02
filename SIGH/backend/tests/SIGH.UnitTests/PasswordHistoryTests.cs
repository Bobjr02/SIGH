using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using SIGH.Application.Authentication.ChangePassword;
using SIGH.Application.Authentication.ResetPassword;
using SIGH.Application.Options;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Exceptions;
using SIGH.Domain.Repositories;
using SIGH.Infrastructure.Authentication;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.UnitTests;

public class PasswordHistoryTests
{
    private SighDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SighDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new SighDbContext(options);
    }

    private BCryptPasswordHasher _passwordHasher = new();

    [Fact]
    public async Task ChangePassword_ReusingLast3Passwords_ShouldThrowException()
    {
        using var context = CreateDbContext();

        var pwd1 = "Password123!";
        var pwd2 = "Password456!";
        var pwd3 = "Password789!";
        var currentPwd = "CurrentPassword123!";

        var user = new User(Guid.NewGuid())
        {
            FullName = "Detetive Ferreira",
            Email = "detetive.ferreira@policiacivil.sp.gov.br",
            Cpf = "66677788899",
            PasswordHash = _passwordHasher.HashPassword(currentPwd),
            Status = UserStatus.Active
        };

        await context.Users.AddAsync(user);

        // Adicionar 3 senhas no histórico
        await context.PasswordHistories.AddRangeAsync(
            new PasswordHistory { UserId = user.Id, PasswordHash = _passwordHasher.HashPassword(pwd1), CreatedAt = DateTimeOffset.UtcNow.AddDays(-30) },
            new PasswordHistory { UserId = user.Id, PasswordHash = _passwordHasher.HashPassword(pwd2), CreatedAt = DateTimeOffset.UtcNow.AddDays(-20) },
            new PasswordHistory { UserId = user.Id, PasswordHash = _passwordHasher.HashPassword(pwd3), CreatedAt = DateTimeOffset.UtcNow.AddDays(-10) }
        );

        await context.SaveChangesAsync();
        var recentHistories = await context.PasswordHistories
            .Where(ph => ph.UserId == user.Id)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(3)
            .ToListAsync();
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepositoryMock
            .Setup(r => r.GetRecentPasswordHistoriesAsync(user.Id, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentHistories);

        var service = new ChangePasswordService(
            context,
            userRepositoryMock.Object,
            _passwordHasher,
            Options.Create(new PasswordOptions { PasswordHistoryLimit = 3 })
        );

        // Tentar reusar pwd2 (que está no histórico de 3)
        var request = new ChangePasswordRequest(user.Id, currentPwd, pwd2);

        var act = async () => await service.ChangePasswordAsync(request);

        await act.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*A nova senha não pode ser igual a nenhuma das últimas 3 senhas utilizadas.*");
    }

    [Fact]
    public async Task ChangePassword_WithNewUniquePassword_ShouldSucceedAndAddHistory()
    {
        using var context = CreateDbContext();

        var currentPwd = "CurrentPassword123!";
        var newPwd = "BrandNewPassword2026!";

        var user = new User(Guid.NewGuid())
        {
            FullName = "Investigador Costa",
            Email = "investigador.costa@policiacivil.sp.gov.br",
            Cpf = "77788899900",
            PasswordHash = _passwordHasher.HashPassword(currentPwd),
            Status = UserStatus.Active
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepositoryMock
            .Setup(r => r.GetRecentPasswordHistoriesAsync(user.Id, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PasswordHistory>());
        userRepositoryMock
            .Setup(r => r.AddPasswordHistoryAsync(It.IsAny<PasswordHistory>(), It.IsAny<CancellationToken>()))
            .Callback<PasswordHistory, CancellationToken>((history, _) => context.PasswordHistories.Add(history))
            .Returns(Task.CompletedTask);

        var service = new ChangePasswordService(
            context,
            userRepositoryMock.Object,
            _passwordHasher,
            Options.Create(new PasswordOptions { PasswordHistoryLimit = 3 })
        );

        var request = new ChangePasswordRequest(user.Id, currentPwd, newPwd);

        var response = await service.ChangePasswordAsync(request);

        response.Should().NotBeNull();
        response.Success.Should().BeTrue();

        var histories = await context.PasswordHistories.Where(ph => ph.UserId == user.Id).ToListAsync();
        histories.Should().NotBeEmpty();

        var updatedUser = await context.Users.FindAsync(user.Id);
        _passwordHasher.VerifyPassword(newPwd, updatedUser!.PasswordHash).Should().BeTrue();
    }
}
