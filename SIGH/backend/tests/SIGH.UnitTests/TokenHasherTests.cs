using FluentAssertions;
using SIGH.Infrastructure.Authentication;
using Xunit;

namespace SIGH.UnitTests;

public class TokenHasherTests
{
    private readonly Sha256TokenHasher _hasher = new();

    [Fact]
    public void HashToken_ShouldReturnDeterministicSha256Hash()
    {
        var rawToken = "sample-raw-refresh-token-12345";

        var hash1 = _hasher.HashToken(rawToken);
        var hash2 = _hasher.HashToken(rawToken);

        hash1.Should().NotBeNullOrWhiteSpace();
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_DifferentTokens_ShouldReturnDifferentHashes()
    {
        var hash1 = _hasher.HashToken("token-1");
        var hash2 = _hasher.HashToken("token-2");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashToken_WithEmptyInput_ShouldReturnEmptyString()
    {
        _hasher.HashToken("").Should().BeEmpty();
    }
}
