using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SIGH.Application.Authentication.ForgotPassword;
using SIGH.Application.Authentication.PasswordTokens;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Email;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Application.Security;
using SIGH.Domain.Entities;
using SIGH.Domain.Enums;
using SIGH.Domain.Repositories;
using Xunit;

namespace SIGH.UnitTests.Authentication;

/// <summary>"Esqueci a senha": o request segue o mesmo caminho exista ou não o e-mail.</summary>
public class ForgotPasswordConstantPathTests
{
    private sealed class RecordingQueue : IPasswordResetRequestQueue
    {
        public List<PasswordResetRequestJob> Jobs { get; } = new();
        public bool TryEnqueue(PasswordResetRequestJob job) { Jobs.Add(job); return true; }
        public IAsyncEnumerable<PasswordResetRequestJob> ReadAllAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private static ForgotPasswordService CreateService(RecordingQueue queue)
    {
        var request = new Mock<IRequestContext>();
        request.SetupGet(r => r.IpAddress).Returns("10.0.0.9");
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero));
        return new ForgotPasswordService(queue, request.Object, clock.Object, NullLogger<ForgotPasswordService>.Instance);
    }

    [Fact]
    public void TheRequestHandler_DoesNotDependOnUsersTheDatabaseOrEmail()
    {
        // Sem repositório, DbContext ou outbox no construtor: o request não tem como consultar se a conta existe.
        var parameters = typeof(ForgotPasswordService).GetConstructors().Single().GetParameters().Select(p => p.ParameterType).ToList();

        parameters.Should().NotContain(new[] { typeof(IUserRepository), typeof(IApplicationDbContext), typeof(IEmailOutbox), typeof(IPasswordTokenService) });
    }

    [Fact]
    public async Task ExistingAndUnknownEmails_EnqueueOneJobAndReturnTheSameResponse()
    {
        var queue = new RecordingQueue();
        var service = CreateService(queue);

        var known = await service.ForgotPasswordAsync(new ForgotPasswordRequest("  Admin@SIGH.com "));
        var unknown = await service.ForgotPasswordAsync(new ForgotPasswordRequest("nao.existe@sigh.com"));

        known.Should().Be(unknown);
        known.Message.Should().Be(ForgotPasswordService.GenericMessage);
        queue.Jobs.Select(j => j.Email).Should().Equal("admin@sigh.com", "nao.existe@sigh.com");
        queue.Jobs.Should().OnlyContain(j => j.IpAddress == "10.0.0.9");
    }

    [Fact]
    public async Task AFullQueue_StillReturnsTheGenericResponse()
    {
        var queue = new Mock<IPasswordResetRequestQueue>();
        queue.Setup(q => q.TryEnqueue(It.IsAny<PasswordResetRequestJob>())).Returns(false);
        var service = new ForgotPasswordService(queue.Object, Mock.Of<IRequestContext>(), Mock.Of<IDateTimeProvider>(), NullLogger<ForgotPasswordService>.Instance);

        (await service.ForgotPasswordAsync(new ForgotPasswordRequest("x@sigh.com"))).Message.Should().Be(ForgotPasswordService.GenericMessage);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(UserStatus.Active, true)]
    [InlineData(UserStatus.PendingFirstAccess, true)]
    [InlineData(UserStatus.Inactive, false)]
    [InlineData(UserStatus.Suspended, false)]
    public async Task TheWorker_DecidesWhetherToSendTheEmail(UserStatus? status, bool sends)
    {
        var users = new Mock<IUserRepository>();
        var user = status is null ? null : new User { FullName = "Ana", Email = "ana@sigh.com", Status = status.Value };
        users.Setup(u => u.GetByEmailAsync("ana@sigh.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var tokens = new Mock<IPasswordTokenService>();
        tokens.Setup(t => t.IssueAsync(It.IsAny<Guid>(), PasswordTokenPurpose.PasswordReset, It.IsAny<CancellationToken>(), "10.0.0.9")).ReturnsAsync("tok");
        tokens.Setup(t => t.LifetimeOf(PasswordTokenPurpose.PasswordReset)).Returns(TimeSpan.FromMinutes(30));
        var outbox = new Mock<IEmailOutbox>();
        var processor = new PasswordResetRequestProcessor(Mock.Of<IApplicationDbContext>(), users.Object, tokens.Object, outbox.Object,
            Mock.Of<ISecurityAuditLogger>(), Options.Create(new AppOptions { PublicBaseUrl = "https://sigh.test" }));

        var sent = await processor.ProcessAsync(new PasswordResetRequestJob("ana@sigh.com", "10.0.0.9", DateTimeOffset.UtcNow));

        sent.Should().Be(sends);
        outbox.Verify(o => o.Enqueue("ana@sigh.com", It.Is<EmailContent>(c => c.TextBody.Contains("https://sigh.test/reset-password?token=tok"))),
            sends ? Times.Once() : Times.Never());
    }
}
