using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SIGH.Application.Email;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Domain.Entities;
using SIGH.Domain.Notifications.Entities;
using SIGH.Domain.Repositories;
using SIGH.Infrastructure.Email;
using Xunit;

namespace SIGH.UnitTests.Email;

/// <summary>Worker da outbox: envio, retentativas com backoff (máximo 5) e registro do erro.</summary>
public class EmailOutboxWorkerTests
{
    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class InMemoryStore : IEmailOutboxStore
    {
        public List<EmailOutboxMessage> Messages { get; } = new();
        public int Saves { get; private set; }

        public Task<IReadOnlyList<EmailOutboxMessage>> GetDueAsync(DateTimeOffset now, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmailOutboxMessage>>(Messages
                .Where(m => m.Status == EmailOutboxStatus.Pending && m.NextAttemptAt <= now)
                .OrderBy(m => m.NextAttemptAt).Take(take).ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Saves++;
            return Task.CompletedTask;
        }
    }

    private sealed class ScriptedSender : IEmailSender
    {
        private readonly Func<OutgoingEmail, bool> _fails;
        public List<OutgoingEmail> Delivered { get; } = new();
        public int Calls { get; private set; }

        public ScriptedSender(Func<OutgoingEmail, bool> fails) => _fails = fails;

        public Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (_fails(email)) throw new InvalidOperationException("SMTP indisponível (421)");
            Delivered.Add(email);
            return Task.CompletedTask;
        }
    }

    private readonly FakeClock _clock = new();
    private readonly InMemoryStore _store = new();

    private EmailOutboxProcessor CreateProcessor(IEmailSender sender, int maxAttempts = 5) =>
        new(_store, sender, _clock,
            Options.Create(new EmailOptions { Outbox = new EmailOutboxOptions { MaxAttempts = maxAttempts, InitialRetryDelaySeconds = 30, MaxRetryDelaySeconds = 3600, BatchSize = 10 } }),
            NullLogger<EmailOutboxProcessor>.Instance);

    private EmailOutboxMessage Enqueue(string to = "ana@sigh.test")
    {
        var message = EmailOutboxMessage.Create(to, "Assunto", "<p>link?token=segredo</p>", "link?token=segredo", "PasswordReset", _clock.UtcNow);
        _store.Messages.Add(message);
        return message;
    }

    [Fact]
    public async Task Success_MarksSentAndRemovesTheBody()
    {
        var message = Enqueue();
        var sender = new ScriptedSender(_ => false);

        var sent = await CreateProcessor(sender).ProcessDueAsync();

        sent.Should().Be(1);
        sender.Delivered.Should().ContainSingle().Which.TextBody.Should().Contain("token=segredo");
        message.Status.Should().Be(EmailOutboxStatus.Sent);
        message.SentAt.Should().Be(_clock.UtcNow);
        message.Attempts.Should().Be(1);
        message.TextBody.Should().NotContain("segredo");
        message.HtmlBody.Should().NotContain("segredo");
    }

    [Fact]
    public async Task Failures_AreRetriedWithExponentialBackoff_UpToFiveAttempts_ThenMarkedFailed()
    {
        var message = Enqueue();
        var sender = new ScriptedSender(_ => true);
        var processor = CreateProcessor(sender);
        var delays = new List<TimeSpan>();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var before = _clock.UtcNow;
            await processor.ProcessDueAsync();

            message.Attempts.Should().Be(attempt);
            message.LastError.Should().Contain("SMTP indisponível");
            if (attempt < 5)
            {
                message.Status.Should().Be(EmailOutboxStatus.Pending);
                delays.Add(message.NextAttemptAt - before);

                // Antes do horário agendado nada acontece.
                await processor.ProcessDueAsync();
                message.Attempts.Should().Be(attempt);

                _clock.UtcNow = message.NextAttemptAt;
            }
        }

        delays.Should().Equal(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(240));
        message.Status.Should().Be(EmailOutboxStatus.Failed);
        message.TextBody.Should().NotContain("segredo");
        sender.Calls.Should().Be(5);

        // Falha definitiva: não é mais tentada.
        _clock.UtcNow = _clock.UtcNow.AddDays(1);
        await processor.ProcessDueAsync();
        sender.Calls.Should().Be(5);
    }

    [Fact]
    public async Task AFailingMessage_DoesNotBlockTheOthers()
    {
        var bad = Enqueue("quebrado@sigh.test");
        var good = Enqueue("ok@sigh.test");
        var sender = new ScriptedSender(e => e.To == "quebrado@sigh.test");

        await CreateProcessor(sender).ProcessDueAsync();

        bad.Status.Should().Be(EmailOutboxStatus.Pending);
        good.Status.Should().Be(EmailOutboxStatus.Sent);
        _store.Saves.Should().Be(2, "cada mensagem é gravada logo após a tentativa");
    }

    [Fact]
    public void RetryDelay_IsCapped()
    {
        var processor = CreateProcessor(new ScriptedSender(_ => false));

        processor.RetryDelay(20).Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task Dispatcher_RunOnce_ProcessesTheOutboxThroughTheScopedProcessor()
    {
        Enqueue();
        var sender = new ScriptedSender(_ => false);
        var services = new ServiceCollection()
            .AddSingleton<IEmailOutboxStore>(_store)
            .AddSingleton<IEmailSender>(sender)
            .AddSingleton<IDateTimeProvider>(_clock)
            .AddSingleton(Options.Create(new EmailOptions()))
            .AddLogging()
            .AddScoped<EmailOutboxProcessor>()
            .BuildServiceProvider();

        var dispatcher = new EmailOutboxDispatcher(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EmailOptions { Enabled = true }),
            NullLogger<EmailOutboxDispatcher>.Instance);

        (await dispatcher.RunOnceAsync(CancellationToken.None)).Should().Be(1);
        sender.Delivered.Should().ContainSingle();
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task DeadlineAlerts_RespectEmailSendDeadlineAlerts(bool enabled, int expected)
    {
        var outbox = new Mock<IEmailOutbox>();
        var users = new Mock<IUserRepository>();
        var user = new User { FullName = "Ana", Email = "ana@sigh.test" };
        users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var emailer = new DeadlineAlertEmailer(outbox.Object, users.Object,
            Options.Create(new EmailOptions { SendDeadlineAlerts = enabled }),
            Options.Create(new AppOptions { PublicBaseUrl = "https://sigh.test" }));

        await emailer.EnqueueAsync(Notification.Create(Guid.NewGuid(), user.Id, "Prazo", "Vence amanhã", dueDate: _clock.UtcNow));

        outbox.Verify(o => o.Enqueue("ana@sigh.test", It.Is<EmailContent>(c =>
            c.Template == EmailTemplates.DeadlineAlertTemplate && c.TextBody.Contains("https://sigh.test/notifications"))), Times.Exactly(expected));
    }
}
