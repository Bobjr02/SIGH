using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using SIGH.Infrastructure.Authentication;
using Xunit;

namespace SIGH.UnitTests.Authentication;

public class CurrentUserServiceTests
{
    [Fact]
    public void WithoutHttpContext_UserIsNull()
    {
        var service = new CurrentUserService(new HttpContextAccessor());

        service.UserId.Should().BeNull();
        service.UserEmail.Should().BeNull();
        service.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void WithAnonymousRequest_UserIsNull()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        new CurrentUserService(accessor).UserId.Should().BeNull();
    }

    [Fact]
    public void WithAuthenticatedRequest_ReadsNameIdentifierAndEmail()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "ana@sigh.test")
        }, authenticationType: "Bearer");
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };

        var service = new CurrentUserService(accessor);

        service.UserId.Should().Be(userId);
        service.UserEmail.Should().Be("ana@sigh.test");
        service.IsAuthenticated.Should().BeTrue();
    }
}
