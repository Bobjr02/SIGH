using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SIGH.Application.Options;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Infrastructure.Authentication;
using Xunit;

namespace SIGH.UnitTests;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ShouldReturnValidJwtWithExpectedClaims()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "SIGH_Issuer",
            Audience = "SIGH_Audience",
            SecretKey = "SuperSecretKeyForTestingJwtTokenGeneration123!",
            ExpiryInMinutes = 60
        });

        var generator = new JwtTokenGenerator(jwtOptions);

        var user = new User(Guid.NewGuid())
        {
            FullName = "Carlos Silva",
            Email = "carlos.silva@policiacivil.sp.gov.br",
            Cpf = "12345678901",
            Status = UserStatus.Active
        };

        var roles = new[] { "Administrador" };
        var permissions = new[] { "USERS_CREATE", "USERS_READ" };

        var tokenString = generator.GenerateToken(user, roles, permissions);

        tokenString.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        jwtToken.Issuer.Should().Be("SIGH_Issuer");
        jwtToken.Audiences.Should().Contain("SIGH_Audience");

        jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id.ToString());
        jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value.Should().Be(user.Email);
        jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value.Should().Be("Administrador");
        jwtToken.Claims.Where(c => c.Type == "permission").Select(c => c.Value).Should().Contain("USERS_CREATE");
    }
}
