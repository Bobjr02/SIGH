using FluentAssertions;
using SIGH.Infrastructure.Authentication;
using Xunit;

namespace SIGH.UnitTests;

public class PasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldGenerateNonEmptyHash()
    {
        var password = "StrongPassword123!";

        var hash = _hasher.HashPassword(password);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var password = "MySecretPassword2026!";
        var hash = _hasher.HashPassword(password);

        var result = _hasher.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        var password = "MySecretPassword2026!";
        var hash = _hasher.HashPassword(password);

        var result = _hasher.VerifyPassword("WrongPassword123!", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithNullOrEmptyInputs_ShouldReturnFalse()
    {
        _hasher.VerifyPassword("", "").Should().BeFalse();
        _hasher.VerifyPassword("password", "").Should().BeFalse();
    }
}
